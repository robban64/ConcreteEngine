#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Internal;
using Silk.NET.Assimp;
using static ConcreteEngine.Engine.Assets.Models.Loader.AssimpImporter.ImportModelUtils;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;

#endregion

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

    private readonly Dictionary<string, IntPtr> _nodeMap = new(16);

    internal ModelAssimpImporter(AssetGfxUploader gfxUploader, ModelLoaderDataTable dataTable, ModelLoaderState state)
    {
        _gfxUploader = gfxUploader;
        _dataTable = dataTable;
        _state = state;
        _meshProcessor = new AssimpMeshProcessor(_dataTable);
        _materialProcessor = new AssimpMaterialProcessor(_state);
        _animationProcessor = new AssimpAnimationProcessor(_dataTable, _state);
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

        _nodeMap.Clear();

        try
        {
            // Start the loading
            ProcessScene(scene);
        }
        finally
        {
            _nodeMap.Clear();
            _assimp.ReleaseImport(scene);
        }

        return true;
    }

    private unsafe void ProcessScene(AssimpScene* scene)
    {
        _state.MightBeAnimated = scene->MNumAnimations > 0 || scene->MNumSkeletons > 0;

        Matrix4x4.Invert(scene->MRootNode->MTransformation, out var invRoot);
        _dataTable.InvRootTransform = Matrix4x4.Transpose(invRoot);

        PreProcessScene(scene);
        MapNodes(scene->MRootNode);
        RegisterAllBones(scene);

        int startIdx = 0;
        TraverseNode(scene->MRootNode, scene, ref startIdx, Matrix4x4.Identity);

        _state.HasAnimationChannels = _animationProcessor.HasAnimationChannels(scene);

        if (_state.HasAnimationChannels)
        {
            _animationProcessor.ProcessSceneAnimations(scene);
        }

        _dataTable.CalculateBoundingBox(_state.MeshCount);

        if (scene->MNumMaterials > 0) _materialProcessor.ProcessSceneMaterials(scene);
    }

    private unsafe void MapNodes(AssimpNode* node)
    {
        _nodeMap[node->MName.AsString] = (IntPtr)node;
        for (int i = 0; i < node->MNumChildren; i++)
            MapNodes(node->MChildren[i]);
    }

    private unsafe void RegisterAllBones(AssimpScene* scene)
    {
        for (uint m = 0; m < scene->MNumMeshes; m++)
        {
            var mesh = scene->MMeshes[m];
            for (uint b = 0; b < mesh->MNumBones; b++)
            {
                var boneName = mesh->MBones[b]->MName.AsString;
                RegisterBoneRecursive(boneName);
            }
        }

        return;

        void RegisterBoneRecursive(string name)
        {
            if (_state.TryGetBoneIndex(name, out _)) return;
            if (_nodeMap.TryGetValue(name, out IntPtr nodePtr))
            {
                var node = (AssimpNode*)nodePtr;
                if (node->MParent != null)
                    RegisterBoneRecursive(node->MParent->MName.AsString);
            }

            int idx = _state.BoneCount;
            _state.AppendBone(name, idx);
        }
    }

    private unsafe void PreProcessScene(AssimpScene* scene)
    {
        TraverseTranspose(scene->MRootNode);
        TransposeBones();
        return;

        void TransposeBones()
        {
            for (int j = 0; j < scene->MNumMeshes; j++)
            {
                var mesh = scene->MMeshes[j];
                for (int b = 0; b < mesh->MNumBones; b++)
                {
                    var bone = mesh->MBones[b];
                    _assimp!.TransposeMatrix4(&bone->MOffsetMatrix);
                }
            }
        }

        void TraverseTranspose(AssimpNode* currentNode)
        {
            for (int i = 0; i < currentNode->MNumChildren; i++)
            {
                var node = currentNode->MChildren[i];
                _assimp!.TransposeMatrix4(&node->MTransformation);
                TraverseTranspose(node);
            }
        }
    }

    private unsafe void TraverseNode(AssimpNode* node, AssimpScene* scene, ref int traverseIndex, in Matrix4x4 parent)
    {
        var nodeTransform = node->MTransformation;
        var local = nodeTransform * parent;

        MeshCreationInfo info;

        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var meshIndex = (int)node->MMeshes[i];

            if (!_state.HasProcessedMeshIndex(meshIndex, out info))
            {
                var m = scene->MMeshes[meshIndex];

                if (m->MNumBones > 0)
                    _animationProcessor.ProcessBoneData(m);

                info = _meshProcessor.LoadAndUploadMesh(m, _gfxUploader, _state.MightBeAnimated);
                _state.AppendMeshInfo(m->MName.AsString, meshIndex, info);
            }

            var mesh = scene->MMeshes[meshIndex];
            var slot = (int)scene->MMeshes[i]->MMaterialIndex;
            var vertexCount = (int)mesh->MNumVertices;

            var writer = _dataTable.WriteMeshParts();
            BoundingBox.FromPoints(new Span<Vector3>(mesh->MVertices, vertexCount), out var bounds);

            writer.Fill(traverseIndex, slot, info, in bounds, in local);

            traverseIndex++;
        }


        // Process children
        for (var i = 0; i < node->MNumChildren; i++)
            TraverseNode(node->MChildren[i], scene, ref traverseIndex, in local);


        if (_state.TryGetBoneIndex(node->MName.AsString, out int index))
        {
            _dataTable.NodeTransforms[index] = nodeTransform;
        }
        else if (node->MNumMeshes > 0)
        {
        }
    }


    internal void Teardown()
    {
        _assimp?.Dispose();
        _assimp = null;
    }
}