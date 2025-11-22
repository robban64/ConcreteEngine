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
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.Loader;

internal sealed class MeshImporter
{
    //PostProcessSteps.PreTransformVertices | 
    private const PostProcessSteps Steps =
        PostProcessSteps.Triangulate |
        PostProcessSteps.SortByPrimitiveType |
        PostProcessSteps.JoinIdenticalVertices |
        PostProcessSteps.GenerateSmoothNormals |
        PostProcessSteps.ImproveCacheLocality |
        PostProcessSteps.CalculateTangentSpace |
        PostProcessSteps.OptimizeMeshes |
        PostProcessSteps.FlipUVs |
        PostProcessSteps.LimitBoneWeights;


    private const int DefaultCapacity = 8192;
    private const int DefaultBoneTransformsCapacity = 64;
    private const int MaxBoneTransformCapacity = 128;
    private const int MaxParts = 8;

    private Assimp? _assimp;

    private uint[] _indices = new uint[DefaultCapacity];
    private Vertex3D[] _vertices = new Vertex3D[DefaultCapacity];
    private Vertex3DSkinned[] _verticesSkinned = new Vertex3DSkinned[DefaultCapacity];

    private int _boneCount = 0;
    private Matrix4x4[] _boneTransforms = new Matrix4x4[DefaultBoneTransformsCapacity];
    private SkinningData[] _skinningData = new SkinningData[DefaultCapacity];

    private readonly Dictionary<string, int> _boneMapping = new(8);

    private readonly MeshPartImportResult[] _parts = new MeshPartImportResult[MaxParts];
    private readonly Matrix4x4[] _partTransforms = new Matrix4x4[MaxParts];
    private readonly List<string> _meshNames = new(MaxParts);

    private Matrix4x4 _invRootTransform;
    private BoundingBox _modelBounds;

    private Action<MeshUploadData<Vertex3D>> _uploadMesh;
    private Action<MeshUploadData<Vertex3DSkinned>> _uploadAnimatedMesh;

    internal MeshImporter(Action<MeshUploadData<Vertex3D>> uploadMesh,
        Action<MeshUploadData<Vertex3DSkinned>> uploadAnimatedMesh)
    {
        _uploadMesh = uploadMesh;
        _uploadAnimatedMesh = uploadAnimatedMesh;
        FillDefaultSkinningData();
    }


    public void ClearCache()
    {
        _meshNames.Clear();

        _vertices = null!;
        _indices = null!;
        _skinningData = null!;
        _verticesSkinned = null!;
        _boneTransforms = null!;

        _uploadMesh = null!;
        _uploadAnimatedMesh = null!;

        _assimp?.Dispose();
        _assimp = null;
    }

    public unsafe void ImportMesh(string path, out ModelImportResult result, out AnimationImportResult animationResult)
    {
        if (_assimp == null)
            _assimp = Assimp.GetApi();

        var scene = _assimp.ImportFile(path, (uint)Steps);

        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new InvalidOperationException(error);
        }

        //cleanup
        _meshNames.Clear();
        _boneMapping.Clear();
        _boneCount = 0;

        // Load meshes
        TraverseNode(scene->MRootNode, scene, 0, Matrix4x4.Identity);

        var count = _meshNames.Count;
        InvalidOpThrower.ThrowIf(count > _parts.Length, nameof(_parts));
        InvalidOpThrower.ThrowIf(count > _partTransforms.Length, nameof(_partTransforms));

        var parts = _parts.AsSpan(0, count);
        var partTransforms = _partTransforms.AsSpan(0, count);

        CalculateBoundingBox(count, parts, out _modelBounds);
        MatrixMath.InvertAffine(in scene->MRootNode->MTransformation, out _invRootTransform);

        result = new ModelImportResult(CollectionsMarshal.AsSpan(_meshNames), parts, partTransforms, ref _modelBounds);

        if (_boneCount > 0)
        {
            animationResult = new AnimationImportResult(
                _boneTransforms.AsSpan(0, count),
                ref _invRootTransform,
                _boneMapping.AsReadOnly());

            return;
        }

        animationResult = default;
    }

    private void CalculateBoundingBox(int count, Span<MeshPartImportResult> parts, out BoundingBox bounds)
    {
        bounds = parts[0].Bounds;
        for (var i = 1; i < count; i++)
        {
            BoundingBox.Merge(in bounds, in parts[i].Bounds, out bounds);
        }
    }

    private unsafe void TraverseNode(AssimpNode* node, AssimpScene* scene, int index, in Matrix4x4 parent)
    {
        var current = node->MTransformation * parent;
        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var meshIndex = node->MMeshes[i];
            var mesh = scene->MMeshes[meshIndex];
            var meshData = LoadMeshData(mesh);

            BoundingBox.FromPoints(new Span<Vector3>(mesh->MVertices, (int)mesh->MNumVertices), out var box);

            ref var it = ref _parts[index + i];
            it.MaterialSlot = (int)scene->MMeshes[i]->MMaterialIndex;
            it.CreationInfo = meshData;

            it.Bounds = box;
            _partTransforms[i] = current;
            _meshNames.Add(mesh->MName.AsString);

            if (_boneCount > 0)
                FillDefaultSkinningData((int)mesh->MNumVertices);
        }

        var idx = index + (int)node->MNumMeshes;

        // Process children
        for (uint i = 0; i < node->MNumChildren; i++)
        {
            TraverseNode(node->MChildren[i], scene, idx, in current);
        }
    }


    private unsafe MeshCreationInfo LoadMeshData(AssimpMesh* mesh)
    {
        var vertexCount = (int)mesh->MNumVertices;
        var indexCount = (int)(mesh->MNumFaces * 3);

        EnsureCapacity(vertexCount, indexCount);

        bool isAnimated = false;
        if (mesh->MNumBones > 0)
        {
            EnsureSkinnedCapacity(vertexCount);
            isAnimated = true;
        }

        var vRes = _vertices.AsSpan(0, vertexCount);
        var iRes = _indices.AsSpan(0, indexCount);

        WriteIndices(mesh, iRes);
        var info = new MeshCreationInfo();

        if (!isAnimated)
        {
            WriteVertices(mesh, vRes);
            _uploadMesh(new MeshUploadData<Vertex3D>(vRes, iRes, ref info));
            return info;
        }
        

        var verticesSkinnedRes = _verticesSkinned.AsSpan(0, vertexCount);
        var skinnedData = _skinningData.AsSpan(0, vertexCount);

        ProcessAnimatedMesh(mesh);
        WriteVerticesSkinned(mesh, verticesSkinnedRes, skinnedData);

        _uploadAnimatedMesh(new MeshUploadData<Vertex3DSkinned>(verticesSkinnedRes, iRes, ref info));
        Console.WriteLine(info);

        return info;


        static void WriteVertices(AssimpMesh* mesh, Span<Vertex3D> vertices)
        {
            ref var v0 = ref MemoryMarshal.GetReference(vertices);
            var count = mesh->MNumVertices;
            for (int i = 0; i < count; i++)
            {
                ref var v = ref Unsafe.Add(ref v0, i);
                v.Position = mesh->MVertices[i];
                v.Normal = mesh->MNormals[i];
                v.Tangent = mesh->MTangents[i];
                v.TexCoords = mesh->MTextureCoords[0][i].ToVec2();
            }
        }

        static void WriteIndices(AssimpMesh* mesh, Span<uint> indices)
        {
            var idx = 0;
            for (int i = 0; i < mesh->MNumFaces; i++)
            {
                var face = mesh->MFaces[i];
                indices[idx++] = face.MIndices[0];
                indices[idx++] = face.MIndices[1];
                indices[idx++] = face.MIndices[2];
            }
        }

        static void WriteVerticesSkinned(AssimpMesh* mesh, Span<Vertex3DSkinned> result, ReadOnlySpan<SkinningData> skinned)
        {
            Debug.Assert(result.Length == skinned.Length);
            
            var count = mesh->MNumVertices;
            for (int i = 0; i < count; i++)
            {
                ref readonly var skinnedVertex = ref skinned[i];
                ref var v = ref result[i];
                v.Position = mesh->MVertices[i];
                v.Normal = mesh->MNormals[i];
                v.Tangent = mesh->MTangents[i];
                v.TexCoords = mesh->MTextureCoords[0][i].ToVec2();
                v.BoneIndices = skinnedVertex.BoneIndices;
                v.BoneWeights = skinnedVertex.BoneWeights;

            }
            /*
        for (var i = 0; i < result.Length; i++)
        {
            ref readonly var skinnedVertex = ref skinned[i];
            ref var it = ref result[i];

            Unsafe.Write(Unsafe.AsPointer(ref it.Position), vertex);
            it.BoneIndices = skinnedVertex.BoneIndices;
            it.BoneWeights = skinnedVertex.BoneWeights;


            it.Position = vertex.Position;
            it.TexCoords = vertex.TexCoords;
            it.Normal = vertex.Normal;
            it.Tangent = vertex.Tangent;
           
            }
             */
        }
    }

    private unsafe void ProcessAnimatedMesh(AssimpMesh* mesh)
    {
        var skinningData = _skinningData.AsSpan(0, (int)mesh->MNumVertices);
        for (int i = 0; i < mesh->MNumBones; i++)
        {
            var boneIndex = 0;

            ref var bone = ref mesh->MBones[i];
            var name = bone->MName.AsString;

            if (_boneMapping.TryGetValue(name, out int value))
            {
                boneIndex = value;
            }
            else
            {
                boneIndex = _boneCount++;
                _boneTransforms[boneIndex] = bone->MOffsetMatrix;
                if (_boneTransforms.Length < boneIndex)
                {
                    InvalidOpThrower.ThrowIf(_boneTransforms.Length >= MaxBoneTransformCapacity,
                        nameof(_boneTransforms.Length));

                    Array.Resize(ref _boneTransforms, MaxBoneTransformCapacity);
                }

                _boneMapping.Add(name, boneIndex);
            }

            for (int j = 0; j < 4; j++)
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

        FillDefaultSkinningData();
    }

    private void FillDefaultSkinningData(int? vertexCount = null)
    {
        var skinData = new SkinningData { BoneWeights = default, BoneIndices = new Int4(-1, -1, -1, -1) };
        if (vertexCount is { } count)
        {
            ArgumentOutOfRangeException.ThrowIfZero(count);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, _skinningData.Length);

            _skinningData.AsSpan(0, count).Fill(skinData);
            return;
        }

        _skinningData.AsSpan().Fill(skinData);
    }


    // cm->0.01f, mm->0.001f, m->1f
    private static float DecideScale(Vector3 bboxMin, Vector3 bboxMax, float unitScale)
    {
        var size = bboxMax - bboxMin;
        var maxDim = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        return unitScale * (maxDim > 100f ? 0.01f : maxDim < 0.01f ? 0.001f : 1f);
    }
}