#region

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Extensions;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Models.Loader;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using static ConcreteEngine.Engine.Assets.Models.Importer.ModelImportConstants;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;
using AssimpMaterial = Silk.NET.Assimp.Material;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.Importer;

internal sealed class ModelImporter
{
    private Assimp? _assimp;

    private uint[] _indices = new uint[DefaultCapacity];
    private Vertex3D[] _vertices = new Vertex3D[DefaultCapacity];
    private Vertex3DSkinned[] _verticesSkinned = new Vertex3DSkinned[DefaultCapacity];

    private Matrix4x4[] _boneTransforms = new Matrix4x4[DefaultBoneTransformsCapacity];
    private SkinningData[] _skinningData = new SkinningData[DefaultCapacity];

    private readonly MeshPartImportResult[] _parts = new MeshPartImportResult[MaxParts];
    private readonly Matrix4x4[] _partTransforms = new Matrix4x4[MaxParts];

    private readonly List<string> _meshNames = new(MaxParts);
    private readonly Dictionary<int, MeshCreationInfo> _meshIndexToIdMap = new(8);

    private readonly Dictionary<string, int> _boneByName = new(8);

    //private readonly Dictionary<int, string> _boneByIndex = new(8);
    private readonly List<int> _parentIndices = new(8);

    private readonly ModelMaterialImporter _materialImporter = new();

    private Matrix4x4 _invRootTransform;
    private BoundingBox _modelBounds;

    private int _boneCount = 0;

    private AssetGfxUploader _gfxUploader;

    internal ModelImporter(AssetGfxUploader gfxUploader)
    {
        _gfxUploader = gfxUploader;
        FillDefaultSkinningData();
    }


    public unsafe ModelMaterialEmbeddedDescriptor[] ImportMesh(string path, out ModelImportResult result,
        out AnimationImportResult animationResult)
    {
        if (_assimp == null)
            _assimp = Assimp.GetApi();

        var scene = _assimp.ImportFile(path, (uint)AssimpFlags);

        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new InvalidOperationException(error);
        }

        //cleanup
        _meshNames.Clear();
        _meshIndexToIdMap.Clear();

        _boneByName.Clear();
        //_boneByIndex.Clear();
        _parentIndices.Clear();

        _boneCount = 0;
        _modelBounds = default;
        _invRootTransform = Matrix4x4.Identity;
        //

        animationResult = default;
        
        // Load the model
        int meshCount = ProcessSceneMeshes(scene);

        if (_boneByName.Count > 0)
        {
            var animationData = ProcessSceneAnimations(scene);
            animationResult = new AnimationImportResult(
                _boneByName,
                CollectionsMarshal.AsSpan(animationData),
                CollectionsMarshal.AsSpan(_parentIndices),
                _boneTransforms.AsSpan(0, _boneByName.Count),
                ref _invRootTransform
            );
        }

        var materials = Array.Empty<ModelMaterialEmbeddedDescriptor>();
        if (scene->MNumMaterials > 0)
            materials = _materialImporter.ProcessSceneMaterials(scene);


        var parts = _parts.AsSpan(0, meshCount);
        var partTransforms = _partTransforms.AsSpan(0, meshCount);

        ModelImportUtils.CalculateBoundingBox(meshCount, parts, out _modelBounds);
        MatrixMath.InvertAffine(in scene->MRootNode->MTransformation, out _invRootTransform);

        result = new ModelImportResult(CollectionsMarshal.AsSpan(_meshNames), parts, partTransforms, ref _modelBounds);

        return materials;
    }

    private unsafe int ProcessSceneMeshes(AssimpScene* scene)
    {
        // Load meshes
        int startIdx = 0;
        TraverseNode(scene->MRootNode, scene, ref startIdx, Matrix4x4.Identity);

        var count = _meshNames.Count;
        InvalidOpThrower.ThrowIf(count > _parts.Length, nameof(_parts));
        InvalidOpThrower.ThrowIf(count > _partTransforms.Length, nameof(_partTransforms));

        return count;
    }
    
    private unsafe List<ModelAnimationData> ProcessSceneAnimations(AssimpScene* scene)
    {
        if (_boneByName.Count > 0)
        {
            Span<int> defaultData  = stackalloc int[_boneByName.Count];
            defaultData.Fill(-1);
            
            _parentIndices.AddRange(defaultData);
            CollectionsMarshal.AsSpan(_parentIndices).Fill(-1);
            BuildSkeletonHierarchy(scene->MRootNode);
        }

        return ImportAnimations(scene);
    }


    private unsafe MeshCreationInfo LoadMeshData(AssimpMesh* mesh)
    {
        var vertexCount = (int)mesh->MNumVertices;
        var indexCount = (int)(mesh->MNumFaces * 3);

        EnsureCapacity(vertexCount, indexCount);

        bool isAnimated = false;
        if (mesh->MNumBones > 0 && mesh->MNumAnimMeshes > 0)
        {
            //EnsureSkinnedCapacity(vertexCount);
            isAnimated = true;
        }

        var vRes = _vertices.AsSpan(0, vertexCount);
        var iRes = _indices.AsSpan(0, indexCount);

        VertexDataWriter.WriteIndices(mesh, iRes);
        var info = new MeshCreationInfo();

        if (!isAnimated)
        {
            VertexDataWriter.WriteVertices(mesh, vRes);
            _gfxUploader.UploadMesh(new MeshUploadData<Vertex3D>(vRes, iRes, ref info));
            return info;
        }


        var verticesSkinnedRes = _verticesSkinned.AsSpan(0, vertexCount);
        var skinnedData = _skinningData.AsSpan(0, vertexCount);

        VertexDataWriter.WriteVerticesSkinned(mesh, verticesSkinnedRes, skinnedData);

        _gfxUploader.UploadMesh(new MeshUploadData<Vertex3DSkinned>(verticesSkinnedRes, iRes, ref info));

        return info;
    }


    private unsafe void TraverseNode(AssimpNode* node, AssimpScene* scene, ref int traverseIndex, in Matrix4x4 parent)
    {
        MeshCreationInfo info;
        BoundingBox box;

        var current = node->MTransformation * parent;

        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var meshIndex = (int)node->MMeshes[i];

            if (!_meshIndexToIdMap.TryGetValue(meshIndex, out info))
            {
                var m = scene->MMeshes[meshIndex];

                if (m->MNumBones > 0)
                {
                    FillDefaultSkinningData((int)m->MNumVertices);
                    ProcessBoneData(m);
                }
                
                info = LoadMeshData(m);
                _meshIndexToIdMap.Add(meshIndex, info);
                _meshNames.Add(m->MName.AsString);

            }

            var mesh = scene->MMeshes[meshIndex];

            BoundingBox.FromPoints(new Span<Vector3>(mesh->MVertices, (int)mesh->MNumVertices), out box);

            ref var it = ref _parts[traverseIndex];
            it.MaterialSlot = (int)scene->MMeshes[i]->MMaterialIndex;
            it.CreationInfo = info;
            it.Bounds = box;
            _partTransforms[traverseIndex] = current;
            traverseIndex++;
        }

        // Process children
        for (var i = 0; i < node->MNumChildren; i++)
            TraverseNode(node->MChildren[i], scene, ref traverseIndex, in current);
    }


    private unsafe void ProcessBoneData(AssimpMesh* mesh)
    {
        var skinningData = _skinningData.AsSpan(0, (int)mesh->MNumVertices);
        for (var i = 0; i < mesh->MNumBones; i++)
        {
            var boneIndex = 0;
            ref var bone = ref mesh->MBones[i];
            var name = bone->MName.AsString;

            if (_boneByName.TryGetValue(name, out var value))
            {
                boneIndex = value;
            }
            else
            {
                boneIndex = _boneCount++;
                _boneByName.Add(name, boneIndex);
                _boneTransforms[boneIndex] = bone->MOffsetMatrix;

                if (_boneTransforms.Length < boneIndex)
                {
                    InvalidOpThrower.ThrowIf(_boneTransforms.Length >= MaxBoneTransformCapacity,
                        nameof(_boneTransforms.Length));

                    Array.Resize(ref _boneTransforms, MaxBoneTransformCapacity);
                }
            }

            for (var j = 0; j < 4; j++)
            {
                var weight = bone->MWeights[j];
                ref var data = ref skinningData[(int)weight.MVertexId];
                if (data.GetVertexId(j) < 0)
                {
                    data.Set(j, boneIndex, weight.MWeight);
                    break;
                }
            }
        }
    }
    
    private unsafe void BuildSkeletonHierarchy(AssimpNode* node)
    {
        var nodeName = node->MName.AsString;

        // Check: Is this current node a Bone? (Did we find it in Step 1?)
        if (_boneByName.TryGetValue(nodeName, out int myBoneIndex))
        {
            // Check: Who is my parent?
            if (node->MParent != null)
            {
                var parentName = node->MParent->MName.AsString;

                // Is my parent ALSO a bone?
                if (_boneByName.TryGetValue(parentName, out int parentBoneIndex))
                {
                    // Link them!
                    _parentIndices[myBoneIndex] = parentBoneIndex;
                }
                else
                {
                    // My parent exists (e.g., "RootNode"), but it's not a Bone used by the mesh.
                    // So I am a root bone.
                    _parentIndices[myBoneIndex] = -1;
                }
            }
        }

        // Recursion: Check children
        for (uint i = 0; i < node->MNumChildren; i++)
        {
            BuildSkeletonHierarchy(node->MChildren[i]);
        }
    }

    private unsafe List<ModelAnimationData> ImportAnimations(AssimpScene* scene)
    {
        if (scene->MNumAnimations == 0) return [];

        var animationLength = (int)scene->MNumAnimations;
        var animations = new List<ModelAnimationData>(animationLength);

        for (uint i = 0; i < animationLength; i++)
        {
            var aiAnim = scene->MAnimations[i];

            var name = aiAnim->MName.AsString;
            var duration = (float)aiAnim->MDuration;
            var ticksPerSecond = (float)(aiAnim->MTicksPerSecond != 0 ? aiAnim->MTicksPerSecond : 25.0f);

            var animationData = new ModelAnimationData(name, duration, ticksPerSecond);

            for (uint c = 0; c < aiAnim->MNumChannels; c++)
            {
                var channel = aiAnim->MChannels[c];
                var boneName = channel->MNodeName.AsString;

                if (!_boneByName.TryGetValue(boneName, out var index))
                {
                    continue;
                }

                var boneTrack = new BoneTrack();

                // Position
                var posKeys = channel->MPositionKeys;
                var posCount = (int)channel->MNumPositionKeys;

                boneTrack.TranslationTimes = new float[posCount];
                boneTrack.Translations = new Vector3[posCount];
                for (var k = 0; k < posCount; k++)
                {
                    boneTrack.TranslationTimes[k] = (float)posKeys[k].MTime;
                    boneTrack.Translations[k] = posKeys[k].MValue;
                }

                // Rotations
                var rotKeys = channel->MRotationKeys;
                var rotCount = (int)channel->MNumRotationKeys;
                boneTrack.RotationTimes = new float[rotCount];
                boneTrack.Rotations = new Quaternion[rotCount];

                for (var k = 0; k < rotCount; k++)
                {
                    boneTrack.RotationTimes[k] = (float)rotKeys[k].MTime;
                    boneTrack.Rotations[k] = rotKeys[k].MValue;
                }

                // Scales
                var scaleKeys = channel->MScalingKeys;
                var scaleCount = (int)channel->MNumScalingKeys;
                boneTrack.ScaleTimes = new float[rotCount];
                boneTrack.Scales = new Vector3[rotCount];

                for (var k = 0; k < scaleCount; k++)
                {
                    boneTrack.ScaleTimes[k] = (float)scaleKeys[k].MTime;
                    boneTrack.Scales[k] = scaleKeys[k].MValue;
                }

                animationData.BoneTracksMap.Add(index, boneTrack);
            }

            animations.Add(animationData);
        }

        return animations;
    }


    internal void ClearCache()
    {
        _meshNames.Clear();
        _meshIndexToIdMap.Clear();
        _boneByName.Clear();

        _vertices = null!;
        _indices = null!;
        _skinningData = null!;
        _verticesSkinned = null!;
        _boneTransforms = null!;
        _gfxUploader = null!;

        _assimp?.Dispose();
        _assimp = null;
    }

    private void EnsureCapacity(int vertexCount, int indexCount)
    {
        if (_vertices.Length < vertexCount)
        {
            var cap = ArrayUtility.CapacityGrowthPow2(int.Max(vertexCount, 8));
            Array.Resize(ref _vertices, cap);
        }

        if (_indices.Length < indexCount)
        {
            var cap = ArrayUtility.CapacityGrowthPow2(int.Max(indexCount, 8));
            Array.Resize(ref _indices, cap);
        }
    }

    private void EnsureSkinnedCapacity(int vertexCount)
    {
        Debug.Assert(_verticesSkinned.Length == _skinningData.Length);
        if (_verticesSkinned.Length >= vertexCount) return;

        var cap = ArrayUtility.CapacityGrowthPow2(int.Max(vertexCount, 8));
        Array.Resize(ref _verticesSkinned, cap);
        Array.Resize(ref _skinningData, cap);
    }

    private void FillDefaultSkinningData(int? vertexCount = null)
    {
        var skinData = new SkinningData { BoneWeights = default, BoneIndices = new Int4(-1, -1, -1, -1) };
        if (vertexCount is { } count)
        {
            ArgumentOutOfRangeException.ThrowIfZero(count);
            EnsureSkinnedCapacity(count);
            _skinningData.AsSpan(0, count).Fill(skinData);
            return;
        }

        _skinningData.AsSpan().Fill(skinData);
    }
}