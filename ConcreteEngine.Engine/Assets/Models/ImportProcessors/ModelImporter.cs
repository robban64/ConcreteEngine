#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Models.Loader;
using Silk.NET.Assimp;
using static ConcreteEngine.Engine.Assets.Models.ImportProcessors.ImportConstants;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.ImportProcessors;

internal sealed class ModelImporter
{
    private Assimp? _assimp;

    private readonly AssetGfxUploader _gfxUploader;
    private readonly ModelImportDataStore _dataStore;
    private readonly ModelLoaderState _state;

    private readonly MeshProcessor _meshProcessor;
    private readonly ModelMaterialProcessor _materialProcessor;
    private readonly ModelAnimationProcessor _animationProcessor;

    internal ModelImporter(AssetGfxUploader gfxUploader, ModelImportDataStore dataStore, ModelLoaderState state)
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

        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new InvalidOperationException(error);
        }

        try
        {
            // Start the loading
            ProcessScene(scene);
        }
        finally
        {
            _assimp.ReleaseImport(scene);
        }

        return true;
    }

    private unsafe void ProcessScene(AssimpScene* scene)
    {
        _state.MightBeAnimated = scene->MNumAnimations > 0 || scene->MNumSkeletons > 0;
        
        MatrixMath.InvertAffine(in scene->MRootNode->MTransformation, out _dataStore.InvRootTransform);        
        MatrixMath.MultiplyAffine(in _dataStore.InvRootTransform, Matrix4x4.CreateRotationX(-MathF.PI / 2), out _dataStore.InvRootTransform);
        
        int startIdx = 0;
        TraverseNode(scene->MRootNode, scene, ref startIdx, Matrix4x4.Identity);

        _state.HasAnimationChannels = _animationProcessor.HasAnimationChannels(scene);

        if (_state.HasAnimationChannels)
        {
            _animationProcessor.ProcessSceneAnimations(scene);
        }

        ModelImportUtils.CalculateBoundingBox(_state.MeshCount, _dataStore.GetParts(_state.MeshCount),
            out _dataStore.ModelBounds);


        if (scene->MNumMaterials > 0)
            _materialProcessor.ProcessSceneMaterials(scene);

    }


    private unsafe void TraverseNode(AssimpNode* node, AssimpScene* scene, ref int traverseIndex, in Matrix4x4 parent)
    {
        var current = node->MTransformation * parent;
        MeshCreationInfo info;

        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var meshIndex = (int)node->MMeshes[i];

            if (!_state.HasProcessedMeshIndex(meshIndex, out info))
            {
                var m = scene->MMeshes[meshIndex];

                if (m->MNumBones > 0)
                {
                    _animationProcessor.ProcessBoneData(m, in parent);
                }

                info = _meshProcessor.LoadAndUploadMesh(m, _gfxUploader, _state.MightBeAnimated, in parent);
                _state.AppendMeshInfo(m->MName.AsString, meshIndex, info);
            }

            var mesh = scene->MMeshes[meshIndex];
            var slot = (int)scene->MMeshes[i]->MMaterialIndex;
            var vertexCount = (int)mesh->MNumVertices;

            var writer = _dataStore.WriteMeshParts();
            BoundingBox.FromPoints(new Span<Vector3>(mesh->MVertices, vertexCount), out var bounds);
            
            writer.Fill(traverseIndex, slot, info, in bounds, ref current);
            traverseIndex++;
        }

        if(_state.TryGetBoneIndex(node->MName.AsString, out int index)) 
            _dataStore.NodeTransforms[index] = current;

        // Process children
        for (var i = 0; i < node->MNumChildren; i++)
            TraverseNode(node->MChildren[i], scene, ref traverseIndex, in current);
    }

    internal void Teardown()
    {
        _assimp?.Dispose();
        _assimp = null;
    }
}