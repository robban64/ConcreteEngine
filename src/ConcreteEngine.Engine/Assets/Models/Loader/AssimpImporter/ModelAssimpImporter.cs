using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Assets.Internal;
using Silk.NET.Assimp;
using static ConcreteEngine.Engine.Assets.Models.Loader.AssimpImporter.ImportModelUtils;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;

namespace ConcreteEngine.Engine.Assets.Models.Loader.AssimpImporter;

internal sealed class ModelAssimpImporter
{
    private Assimp? _assimp;

    private readonly AssetGfxUploader _gfxUploader;
    private readonly ModelLoaderDataTable _dataTable;
    private readonly ModelLoaderState _state;

    private readonly AssimpMeshProcessor _meshProcessor;
    private readonly AssimpMaterialProcessor _materialProcessor;
    private readonly AssimpAnimationProcessor _animationProcessor;

    private readonly AssimpScenePreProcessor _scenePreProcessor;

    internal ModelAssimpImporter(AssetGfxUploader gfxUploader, ModelLoaderDataTable dataTable, ModelLoaderState state)
    {
        _gfxUploader = gfxUploader;
        _dataTable = dataTable;
        _state = state;
        _meshProcessor = new AssimpMeshProcessor(_dataTable, state);
        _materialProcessor = new AssimpMaterialProcessor(_state);
        _animationProcessor = new AssimpAnimationProcessor(_dataTable, _state);
        _scenePreProcessor = new AssimpScenePreProcessor(_state);
    }


    public unsafe bool ImportMesh(string path)
    {
        _assimp ??= Assimp.GetApi();

        var scene = _assimp.ImportFile(path, (uint)AssimpFlags);


        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new InvalidOperationException(error);
        }

        _scenePreProcessor.Clear();

        try
        {
            // Start the loading
            ProcessScene(scene);
        }
        finally
        {
            _scenePreProcessor.Clear();
            _assimp.ReleaseImport(scene);
        }

        return true;
    }

    private unsafe void ProcessScene(AssimpScene* scene)
    {
        _scenePreProcessor.PreProcessSceneGraph(_assimp!, scene, _dataTable);

        int startIdx = 0;
        TraverseSceneNodes(scene->MRootNode, scene, ref startIdx, Matrix4x4.Identity);

        if (_state.HasAnimationChannels && _state.BoneCount > 0)
        {
            _animationProcessor.ProcessSceneAnimations(scene);
        }

        _dataTable.CalculateBoundingBox(_state.MeshCount);

        if (scene->MNumMaterials == 0) return;

        _materialProcessor.ProcessSceneMaterials(scene);
    }

    private unsafe void TraverseSceneNodes(AssimpNode* node, AssimpScene* scene, ref int traverseIndex,
        in Matrix4x4 parent)
    {
        if (node == null) return;

        var nodeTransform = node->MTransformation;
        MatrixMath.MultiplyAffine(in nodeTransform, in parent, out var world);

        MeshCreationInfo info;
        BoundingBox bounds;
        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var meshIndex = (int)node->MMeshes[i];

            var mesh = scene->MMeshes[meshIndex];
            var slot = (int)scene->MMeshes[i]->MMaterialIndex;
            var vertexCount = (int)mesh->MNumVertices;

            if (!_state.HasProcessedMeshIndex(meshIndex, out info))
            {
                info = _meshProcessor.ProcessAndUploadMeshes(mesh, in world, meshIndex, _gfxUploader, out bounds);
                _dataTable.WriteMeshParts().Fill(meshIndex, slot, info, in bounds, in world);
            }
            //var writer = _dataTable.WriteMeshParts();
            //writer.Fill(traverseIndex, slot, info, in bounds, in local);
            //BoundingBox.FromPoints(new Span<Vector3>(mesh->MVertices, vertexCount), out var bounds);

            traverseIndex++;
        }


        // Process children
        for (var i = 0; i < node->MNumChildren; i++)
        {
            if (node->MChildren[i] == null) continue;
            TraverseSceneNodes(node->MChildren[i], scene, ref traverseIndex, in world);
        }

        if (_state.HasAnimationChannels && _state.BoneCount > 0)
        {
            if (_state.TryGetBoneIndex(node->MName.AsString, out int index))
            {
                _dataTable.NodeTransforms[index] = nodeTransform;
            }
        }
    }


    internal void Teardown()
    {
        _scenePreProcessor.Clear();
        _assimp?.Dispose();
        _assimp = null;
    }
}