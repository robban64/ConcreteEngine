using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpAnimation = Silk.NET.Assimp.Animation;
using static ConcreteEngine.Engine.Assets.Loader.ImporterModel.AssimpUtils;


namespace ConcreteEngine.Engine.Assets.Loader.ImporterModel;

internal sealed unsafe partial class ModelImporter : IDisposable
{
    public readonly MeshScratchpad Scratchpad;
    public readonly Dictionary<string, int> BoneIndexByName = new(BoneLimit);
    public readonly Dictionary<(int meshIndex, int boneOrder), int> BoneIndexByMeshBone = new(BoneLimit);
    public readonly Dictionary<string, IntPtr> NodeMap = new(BoneLimit);

    private Assimp _assimp;

    private AssimpSceneMeta _sceneMeta;

    internal ModelImporter(MeshScratchpad scratchpad)
    {
        ArgumentNullException.ThrowIfNull(scratchpad);
        Scratchpad = scratchpad;
        _assimp = Assimp.GetApi();
    }

    public void Dispose()
    {
        NodeMap.Clear();
        BoneIndexByName.Clear();
        BoneIndexByMeshBone.Clear();

        NodeMap.TrimExcess();
        BoneIndexByName.TrimExcess();
        BoneIndexByMeshBone.TrimExcess();

        _assimp.Dispose();
        _assimp = null!;
    }

    public void Cleanup()
    {
        _sceneMeta = default;
        NodeMap.Clear();
        BoneIndexByName.Clear();
        BoneIndexByMeshBone.Clear();
    }


    public ModelImportContext ImportModel(string name, string path, AssetGfxUploader gfxUploader)
    {
        if (NodeMap.Count > 0 || BoneIndexByName.Count > 0)
            throw new InvalidOperationException();

        var scene = _assimp.ImportFile(path, (uint)AssimpFlags);

        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new InvalidOperationException(error);
        }

        var numMeshes = (int)scene->MNumMeshes;
        if (numMeshes == 0) throw new InvalidOperationException($"Model {name} contains no meshes");


        PreProcessScene(scene);
        var boneCount = RegisterBones(scene);
        _sceneMeta.FromScene(scene, boneCount);

        Span<(int vertexCount, int indexCount)> meshParts = stackalloc (int, int)[numMeshes];

        var ctx = new ModelImportContext(name, path, _sceneMeta.MaterialCount, _sceneMeta.TextureCount)
        {
            Model = RegisterMeshes(scene, meshParts), Animation = MakeAnimation(scene)
        };

        Scratchpad.Begin(meshParts);

        TraverseScene(scene->MRootNode, ctx, Matrix4x4.Identity);

        for (var i = 0; i < _sceneMeta.MeshCount; i++)
            ProcessMesh(scene->MMeshes[i], i, ctx);

        if (ctx.Animation is { } animation)
        {
            for (var i = 0; i < _sceneMeta.AnimationCount; i++)
                ProcessAnimation(scene->MAnimations[i], animation);
        }

        ProcessSceneMaterials(scene, ctx);

        UploadMeshes(gfxUploader, ctx);

        _assimp.FreeScene(scene);
        Scratchpad.End();
        return ctx;
    }

    private void ProcessScene()
    {
        
    }


    private void UploadMeshes(AssetGfxUploader gfxUploader, ModelImportContext ctx)
    {
        var model = ctx.Model;
        var animation = ctx.Animation;

        foreach (var mesh in model.Meshes)
        {
            var info = new MeshCreationInfo();
            if (animation != null)
            {
                var meshSpan = Scratchpad.GetSkinnedMeshSpan(mesh.Info.MeshIndex);
                var payload = new MeshUploadData<VertexSkinned>(meshSpan.Vertices, meshSpan.Indices, ref info);
                gfxUploader.UploadMesh(payload);
            }
            else
            {
                var meshSpan = Scratchpad.GetMeshSpan(mesh.Info.MeshIndex);
                var payload = new MeshUploadData<Vertex3D>(meshSpan.Vertices, meshSpan.Indices, ref info);
                gfxUploader.UploadMesh(payload);
            }

            mesh.MeshId = info.MeshId;
        }

        var bounds = model.Meshes[0].LocalBounds;
        for (var i = 1; i < model.Meshes.Length; i++)
            BoundingBox.Merge(in bounds, in model.Meshes[i].LocalBounds, out bounds);

        model.ModelBounds = bounds;
    }

    private void PreProcessScene(AssimpScene* scene)
    {
        if (_assimp is null) throw new InvalidOperationException(nameof(_assimp));
        _assimp.Matrix4Inverse(&scene->MRootNode->MTransformation);
        _assimp.TransposeMatrix4(&scene->MRootNode->MTransformation);
        TraverseTranspose(_assimp, scene->MRootNode);

        return;

        void TraverseTranspose(Assimp assimp, AssimpNode* currentNode)
        {
            for (var i = 0; i < currentNode->MNumChildren; i++)
            {
                var node = currentNode->MChildren[i];
                NodeMap[node->MName] = (IntPtr)node;
                assimp.TransposeMatrix4(&node->MTransformation);
                TraverseTranspose(assimp, node);
            }
        }
    }

    private int RegisterBones(AssimpScene* scene)
    {
        var numMeshes = (int)scene->MNumMeshes;
        for (var i = 0; i < numMeshes; i++)
        {
            var aiMesh = scene->MMeshes[i];
            var boneCount = aiMesh->MNumBones;
            for (var b = 0; b < boneCount; b++)
            {
                var bone = aiMesh->MBones[b];
                _assimp.TransposeMatrix4(&bone->MOffsetMatrix);
                RegisterBoneRecursive(bone->MName.AsString);
            }
        }

        return BoneIndexByName.Count;

        void RegisterBoneRecursive(string name)
        {
            if (BoneIndexByName.ContainsKey(name)) return;
            if (NodeMap.TryGetValue(name, out var nodePtr))
            {
                var node = (AssimpNode*)nodePtr;
                if (node->MParent != null)
                    RegisterBoneRecursive(node->MParent->MName.AsString);
            }

            BoneIndexByName[name] = BoneIndexByName.Count;
        }
    }

    private static ModelData RegisterMeshes(AssimpScene* scene, Span<(int vertexCount, int indexCount)> meshParts)
    {
        var numMeshes = (int)scene->MNumMeshes;

        var model = new ModelData(numMeshes);
        for (var i = 0; i < numMeshes; i++)
        {
            var meshIndex = (byte)i;

            var aiMesh = scene->MMeshes[meshIndex];
            int vertCount = (int)aiMesh->MNumVertices, faceCount = (int)aiMesh->MNumFaces;
            meshParts[meshIndex] = (vertCount, faceCount * 3);
            model.TotalVertexCount += vertCount;
            model.TotalFaceCount += faceCount;

            byte boneCount = (byte)aiMesh->MNumBones, materialIndex = (byte)aiMesh->MMaterialIndex;
            model.Meshes[meshIndex] = new MeshEntry
            {
                Name = aiMesh->MName.AsString,
                Info = new MeshInfo(vertCount, faceCount, meshIndex, materialIndex, boneCount)
            };
        }

        return model;
    }

    //

    private void TraverseScene(AssimpNode* node, ModelImportContext ctx, in Matrix4x4 parentWorld)
    {
        if (node == null) return;

        var local = node->MTransformation;
        MatrixMath.MultiplyAffine(in local, in parentWorld, out var world);

        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var meshIndex = (int)node->MMeshes[i];
            ctx.Model.WorldTransforms[meshIndex] = world;
        }

        for (var i = 0; i < node->MNumChildren; i++)
        {
            if (node->MChildren[i] == null) continue;
            TraverseScene(node->MChildren[i], ctx, in world);
        }

        if (ctx.Animation is { } animation && BoneIndexByName.TryGetValue(node->MName.AsString, out var boneIndex))
        {
            ref readonly var skeleton = ref animation.SkeletonData;
            skeleton.BindPose[boneIndex] = local;
            //Matrix4x4.Invert(local, out skeleton.InverseBindPose[boneIndex]); // OffsetMatrix

            var parent = node->MParent;
            if (node->MParent != null && BoneIndexByName.TryGetValue(parent->MName.AsString, out var parentIdx))
            {
                skeleton.ParentIndices[boneIndex] = parentIdx;
            }
            else
            {
                //skeleton.InverseBindPose[boneIndex] = local;
                skeleton.ParentIndices[boneIndex] = -1;
            }
        }
    }

    private void ProcessMesh(AssimpMesh* aiMesh, int meshIndex, ModelImportContext ctx)
    {
        if (ctx.Animation == null)
        {
            var meshSpan = Scratchpad.GetMeshSpan(meshIndex);
            WriteIndices(aiMesh, meshSpan.Indices);
            WriteVertices(aiMesh, meshIndex, ctx.Model, meshSpan.Vertices);
        }
        else
        {
            var meshSpan = Scratchpad.GetSkinnedMeshSpan(meshIndex);
            WriteIndices(aiMesh, meshSpan.Indices);
            WriteSkinningData(aiMesh, ctx.Animation, BoneIndexByName, meshSpan.Skinned);
            WriteVerticesSkinned(aiMesh, meshIndex, ctx.Model, meshSpan.Vertices, meshSpan.Skinned);
        }
    }
}