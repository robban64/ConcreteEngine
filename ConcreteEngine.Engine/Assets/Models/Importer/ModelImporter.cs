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
using static ConcreteEngine.Engine.Assets.Models.Importer.Constants;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;
using AssimpMaterial = Silk.NET.Assimp.Material;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.Importer;

internal sealed class ModelImporter
{
    private Assimp? _assimp;

    private readonly AssetGfxUploader _gfxUploader;
    private readonly ModelImportDataStore _dataStore;
    private readonly ModelImportState _state;

    private readonly MeshProcessor _meshProcessor;
    private readonly ModelMaterialProcessor _materialProcessor;
    private readonly ModelAnimationProcessor _animationProcessor;

    internal ModelImporter(AssetGfxUploader gfxUploader, ModelImportDataStore dataStore, ModelImportState state)
    {
        _gfxUploader = gfxUploader;
        _dataStore = dataStore;
        _state = state;
        _meshProcessor = new MeshProcessor(_dataStore);
        _materialProcessor = new ModelMaterialProcessor(_state);
        _animationProcessor = new ModelAnimationProcessor(_dataStore, _state);
    }


    public unsafe bool ImportMesh(string path)
    {
        if (_assimp == null)
            _assimp = Assimp.GetApi();

        var scene = _assimp.ImportFile(path, (uint)AssimpFlags);
        //_assimp.FreeScene();

        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new InvalidOperationException(error);
        }

        //cleanup
        /*
        _meshNames.Clear();
        _meshIndexToIdMap.Clear();

        _boneByName.Clear();
        //_boneByIndex.Clear();
        _parentIndices.Clear();

        _boneCount = 0;
        _hasValidAnimation = false;
        _modelBounds = default;
        _invRootTransform = Matrix4x4.Identity;
        */
        //

        // Load the model
         ProcessSceneMeshes(scene);
        var isAnimated = _animationProcessor.HasAnimationChannels(scene);

        if (isAnimated)
        {
            _animationProcessor.ProcessSceneAnimations(scene);
            MatrixMath.InvertAffine(in scene->MRootNode->MTransformation, out _dataStore.InvRootTransform);
            /* animationResult = new AnimationImportResult(
                 _boneByName,
                 CollectionsMarshal.AsSpan(animationData),
                 CollectionsMarshal.AsSpan(_parentIndices),
                 _boneTransforms.AsSpan(0, _boneByName.Count),
                 ref _invRootTransform
             );*/
        }

        ModelImportUtils.CalculateBoundingBox(_state.MeshCount, _dataStore.GetParts(_state.MeshCount),
            out _dataStore.ModelBounds);


        if (scene->MNumMaterials > 0)
            _materialProcessor.ProcessSceneMaterials(scene);

        /*
        if (isAnimated)
        {
            foreach (var mat in materials)
            {
                mat.IsAnimated = true;
            }
        }
        */

        return true;
    }

    private unsafe void ProcessSceneMeshes(AssimpScene* scene)
    {
        // Load meshes
        int startIdx = 0;
        TraverseNode(scene->MRootNode, scene, ref startIdx, Matrix4x4.Identity);

        //InvalidOpThrower.ThrowIf(count > _parts.Length, nameof(_parts));
        //InvalidOpThrower.ThrowIf(count > _partTransforms.Length, nameof(_partTransforms));
    }


    private unsafe void TraverseNode(AssimpNode* node, AssimpScene* scene, ref int traverseIndex, in Matrix4x4 parent)
    {
        MeshCreationInfo info;

        var current = node->MTransformation * parent;

        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var meshIndex = (int)node->MMeshes[i];

            if (!_state.HasProcessedMeshIndex(meshIndex, out info))
            {
                var m = scene->MMeshes[meshIndex];

                if (m->MNumBones > 0)
                {
                    _animationProcessor.ProcessBoneData(m);
                }

                info = _meshProcessor.LoadAndUploadMesh(m, _gfxUploader, false);
                _state.AppendMeshInfo(m->MName.AsString, meshIndex, info);
            }

            var mesh = scene->MMeshes[meshIndex];
            var slot = (int)scene->MMeshes[i]->MMaterialIndex;
            var vertexCount = (int)mesh->MNumVertices;

            var writer = _dataStore.WriteMeshParts();
            BoundingBox.FromPoints(new Span<Vector3>(mesh->MVertices, vertexCount), out var bounds);
            writer.Fill(traverseIndex, slot, info, in bounds, ref current);
            traverseIndex++;

            /*
            ref var it = ref writer.Parts[traverseIndex];
            it.MaterialSlot = (int)scene->MMeshes[i]->MMaterialIndex;
            it.CreationInfo = info;
            writer.PartTransforms[traverseIndex] = current;
            */
        }

        // Process children
        for (var i = 0; i < node->MNumChildren; i++)
            TraverseNode(node->MChildren[i], scene, ref traverseIndex, in current);
    }

    internal void ClearCache()
    {
        _assimp?.Dispose();
        _assimp = null;
    }
}