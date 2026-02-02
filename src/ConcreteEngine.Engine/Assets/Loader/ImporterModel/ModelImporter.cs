using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Handles;
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
    private readonly MeshScratchpad _scratchpad;
    private readonly Dictionary<string, int> _boneIndexByName = new(BoneLimit);
    private readonly Dictionary<uint, int> _boneIndexByHash = new(BoneLimit);
    private readonly Dictionary<uint, IntPtr> _nodeMap = new(BoneLimit);

    private Assimp _assimp;

    private AssimpSceneMeta _sceneMeta;

    internal ModelImporter(MeshScratchpad scratchpad)
    {
        ArgumentNullException.ThrowIfNull(scratchpad);
        _scratchpad = scratchpad;
        _assimp = Assimp.GetApi();
    }

    public void Dispose()
    {
        _nodeMap.Clear();
        _boneIndexByName.Clear();
        _boneIndexByHash.Clear();
        
        _nodeMap.TrimExcess();
        _boneIndexByName.TrimExcess();
        _boneIndexByHash.TrimExcess();

        _assimp.Dispose();
        _assimp = null!;
    }

    public void Cleanup()
    {
        _sceneMeta = default;
        _nodeMap.Clear();
        _boneIndexByName.Clear();
        _boneIndexByHash.Clear();
    }


    public ModelImportContext ImportModel(string name, string path, AssetGfxUploader gfxUploader)
    {
        if (_nodeMap.Count > 0 || _boneIndexByName.Count > 0 || _boneIndexByHash.Count > 0)
            throw new InvalidOperationException();

        var scene = _assimp.ImportFile(path, (uint)AssimpFlags);

        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new InvalidOperationException(error);
        }

        if ((int)scene->MNumMeshes == 0) throw new InvalidOperationException($"Model {name} contains no meshes");

        PreProcessScene(scene);
        var boneCount = RegisterBones(scene);
        _sceneMeta.FromScene(scene, boneCount);

        Span<(int vertexCount, int indexCount)> meshParts = stackalloc (int, int)[_sceneMeta.MeshCount];

        var ctx = new ModelImportContext(name, path, _sceneMeta.MaterialCount, _sceneMeta.TextureCount)
        {
            Model = RegisterMeshes(scene, meshParts), Animation = MakeAnimation(scene)
        };

        _scratchpad.Begin(meshParts);
        Execute(scene, ctx, gfxUploader);
        _scratchpad.End();

        _assimp.FreeScene(scene);
        return ctx;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Execute(AssimpScene* scene, ModelImportContext ctx, AssetGfxUploader uploader)
    {
        TraverseScene(scene->MRootNode, ctx, Matrix4x4.Identity);

        var meta = _sceneMeta;
        
        for (var i = 0; i < meta.MeshCount; i++)
            ProcessMeshVertices(scene->MMeshes[i], i, ctx);

        if (ctx.Animation is { } animation)
        {
            for (var i = 0; i < meta.AnimationCount; i++)
                ProcessAnimation(scene->MAnimations[i], animation, _boneIndexByHash);
        }

        ProcessMaterials(scene, ctx, meta);

        UploadMeshes(uploader, ctx);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private void PreProcessScene(AssimpScene* scene)
    {
        if (_assimp is null) throw new InvalidOperationException(nameof(_assimp));
        _assimp.Matrix4Inverse(&scene->MRootNode->MTransformation);
        _assimp.TransposeMatrix4(&scene->MRootNode->MTransformation);
        TraverseTranspose(_assimp, scene->MRootNode);

        return;

        [MethodImpl(MethodImplOptions.NoInlining)]
        void TraverseTranspose(Assimp assimp, AssimpNode* currentNode)
        {
            for (var i = 0; i < currentNode->MNumChildren; i++)
            {
                var node = currentNode->MChildren[i];
                _nodeMap[GetNameHash(node->MName)] = (IntPtr)node;
                assimp.TransposeMatrix4(&node->MTransformation);
                TraverseTranspose(assimp, node);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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
                RegisterBoneRecursive(bone->MName);
            }
        }

        return _boneIndexByName.Count;

        [MethodImpl(MethodImplOptions.NoInlining)]
        void RegisterBoneRecursive(AssimpString name)
        {
            var hash = GetNameHash(name);
            if(_boneIndexByHash.ContainsKey(hash)) return;
            if (_nodeMap.TryGetValue(hash, out var nodePtr))
            {
                var node = (AssimpNode*)nodePtr;
                if (node->MParent != null)
                    RegisterBoneRecursive(node->MParent->MName);
            }

            var boneIndex = _boneIndexByName.Count;
            _boneIndexByHash[hash] = boneIndex;
            _boneIndexByName[name.AsString] = boneIndex;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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

            var materialIndex = (byte)aiMesh->MMaterialIndex;
            var info = new MeshInfo(vertCount, faceCount, meshIndex, materialIndex, (ushort)aiMesh->MNumBones);
            model.Meshes[meshIndex] = new MeshEntry(aiMesh->MName.AsString, info);
        }

        return model;
    }

    //

    private void TraverseScene(AssimpNode* node, ModelImportContext ctx, in Matrix4x4 parentWorld)
    {
        if (node == null) return;

        ref var local = ref node->MTransformation;
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

        if (ctx.Animation is { } animation && _boneIndexByHash.TryGetValue(GetNameHash(node->MName), out var boneIndex))
        {
            ref readonly var skeleton = ref animation.SkeletonData;
            skeleton.BindPose[boneIndex] = local;
            //Matrix4x4.Invert(local, out skeleton.InverseBindPose[boneIndex]); // OffsetMatrix

            var parent = node->MParent;
            if (node->MParent != null && _boneIndexByHash.TryGetValue(GetNameHash(parent->MName), out var parentIdx))
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ProcessMeshVertices(AssimpMesh* aiMesh, int meshIndex, ModelImportContext ctx)
    {
        if (ctx.Animation == null)
        {
            var meshSpan = _scratchpad.GetMeshSpan(meshIndex);
            WriteIndices(aiMesh, meshSpan.Indices);
            WriteVertices(aiMesh, meshIndex, ctx.Model, meshSpan.Vertices);
        }
        else
        {
            var meshSpan = _scratchpad.GetSkinnedMeshSpan(meshIndex);
            WriteIndices(aiMesh, meshSpan.Indices);
            WriteSkinningData(aiMesh, ctx.Animation, _boneIndexByHash, meshSpan.Skinned);
            WriteVerticesSkinned(aiMesh, meshIndex, ctx.Model, meshSpan.Vertices, meshSpan.Skinned);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UploadMeshes(AssetGfxUploader gfxUploader, ModelImportContext ctx)
    {
        var model = ctx.Model;
        var animation = ctx.Animation;
        var meshes = model.Meshes;
        foreach (var mesh in meshes)
        {
            MeshId meshId;
            if (animation != null)
            {
                var meshSpan = _scratchpad.GetSkinnedMeshSpan(mesh.Info.MeshIndex);
                var payload = new MeshUploadData<VertexSkinned>(meshSpan.Vertices, meshSpan.Indices);
                meshId = gfxUploader.UploadMesh(payload);
            }
            else
            {
                var meshSpan = _scratchpad.GetMeshSpan(mesh.Info.MeshIndex);
                var payload = new MeshUploadData<Vertex3D>(meshSpan.Vertices, meshSpan.Indices);
                meshId = gfxUploader.UploadMesh(payload);
            }

            if (!meshId.IsValid()) throw new InvalidOperationException("Upload returned invalid MeshId");
            mesh.MeshId = meshId;
        }

        var bounds = meshes[0].LocalBounds;
        for (var i = 1; i < meshes.Length; i++)
            BoundingBox.Merge(in bounds, in meshes[i].LocalBounds, out bounds);

        model.ModelBounds = bounds;
    }
}