using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Engine.Assets.Importer;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Engine.Assets.Loader.Data;
using Silk.NET.Assimp;
using static ConcreteEngine.Engine.Assets.ImporterAssimp.AssimpUtils;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpNode = Silk.NET.Assimp.Node;
using AssimpScene = Silk.NET.Assimp.Scene;

namespace ConcreteEngine.Engine.Assets.ImporterAssimp;

internal sealed unsafe partial class ModelImporter : IDisposable
{
    private Assimp _assimp;
    private AssimpScene* _scene;
    private AssimpSceneMeta _sceneMeta;

    private readonly ModelImportContext _context;

    internal ModelImporter(TextureLoader textureLoader)
    {
        _assimp = Assimp.GetApi();
        _context = new ModelImportContext(textureLoader);
        _context.Reset();
    }


    public void Dispose()
    {
        _context.Reset();
        _assimp.Dispose();
        _assimp = null!;
        _scene = null;
    }

    public void Cleanup()
    {
        _context.Reset();
        _sceneMeta = default;
        if(_scene != null)
            _assimp.FreeScene(_scene);
        _scene = null;

    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private AssimpScene* LoadScene(string path, string filename)
    {
        var buffer = stackalloc char[PathUtils.JoinPathLength];
        var bytes = PathUtils.JoinPath(buffer, path, filename);
        return _assimp.ImportFile(bytes, (uint)AssimpFlags);
    }

    public ModelImportContext StartImport(string name, string path, string filename)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);
        
        var scene = LoadScene(path, filename);
        
        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
            Throwers.InvalidOperation(_assimp.GetErrorStringS());

        if ((int)scene->MNumMeshes == 0)
            Throwers.InvalidOperation($"Model {name} contains no meshes");

        _context.StartContext(name, filename);

        _scene = scene;

        PreProcessScene(scene);

        var boneCount = RegisterBones(scene);
        _sceneMeta.FromScene(scene, boneCount);
        _context.Begin(_sceneMeta, HasAnimationChannels(scene));

        RegisterMeshes(scene, _context.MeshContext);
        return _context;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ImportSceneData()
    {
        var scene = _scene;
        if (scene == null) throw new InvalidOperationException("Scene cannot be null.");

        TraverseScene(scene->MRootNode, _context, Matrix4x4.Identity);

        var meshCount = _sceneMeta.MeshCount;
        for (var i = 0; i < meshCount; i++)
            ProcessMeshVertices(scene->MMeshes[i], i, _context);

        if (_context.IsAnimated)
            ProcessAnimations(scene->MAnimations, _context);

        MaterialModelImporter.ProcessMaterials(scene, _context);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Upload(ModelLoader gfxUploader)
    {
        var isAnimated = _context.IsAnimated;
        var meshCtx = _context.MeshContext;
        var meshes = meshCtx.Meshes.AsSpan(0, meshCtx.MeshCount);
        foreach (var mesh in meshes)
        {
            var meshIndex = mesh.Info.MeshIndex;
            var is16Bit = meshCtx.GetMeshData(meshIndex, out var vertices, out var skinned, out var indices);

            var meshId = isAnimated
                ? gfxUploader.UploadAnimatedMesh(vertices, skinned, indices, is16Bit)
                : gfxUploader.UploadMesh(vertices, indices, is16Bit);

            if (!meshId.IsValid())
                Throwers.InvalidOperation("Upload returned invalid MeshId");

            mesh.SetMeshId(meshId);
        }

        var bounds = meshes[0].Bounds;
        for (var i = 1; i < meshes.Length; i++)
            BoundingBox.Merge(in bounds, in meshes[i].Bounds, out bounds);

        meshCtx.ModelBounds = bounds;
    }


    private void PreProcessScene(AssimpScene* scene)
    {
        if (_assimp is null) throw new InvalidOperationException(nameof(_assimp));
        _assimp.TransposeMatrix4(&scene->MRootNode->MTransformation);
        TraverseTranspose(_assimp, scene->MRootNode, _context);

        return;
        static void TraverseTranspose(Assimp assimp, AssimpNode* currentNode, ModelImportContext ctx)
        {
            var length = currentNode->MNumChildren;
            for (var i = 0; i < length; i++)
            {
                var node = currentNode->MChildren[i];
                ctx.Hashes[ctx.HashIndex] = GetNameHash(node->MName);
                ctx.Nodes[ctx.HashIndex++] = (IntPtr)node;
                assimp.TransposeMatrix4(&node->MTransformation);
                TraverseTranspose(assimp, node, ctx);
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
                RegisterBoneRecursive(bone->MName, _context);
            }
        }

        return _context.BoneIndexByName.Count;

        static void RegisterBoneRecursive(AssimpString name, ModelImportContext ctx)
        {
            var hash = GetNameHash(name);

            if (ctx.TryGetBoneIndex(hash, out _)) return;
            if (ctx.TryGetNode(hash, out var nodePtr))
            {
                var node = (AssimpNode*)nodePtr;
                if (node->MParent != null)
                    RegisterBoneRecursive(node->MParent->MName,ctx);
            }

            var hashIndex = ctx.Hashes.IndexOf(hash);
            var boneIndex = ctx.BoneIndexByName.Count;

            if (hashIndex < 0)
            {
                ctx.Hashes[ctx.HashIndex] = hash;
                hashIndex = ctx.HashIndex++;
            }

            ctx.BoneIndices[hashIndex] = (short)boneIndex;
            ctx.BoneIndexByName[name.AsString] = boneIndex;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RegisterMeshes(AssimpScene* scene, MeshImportContext ctx)
    {
        if(ctx.MeshCount == 0) Throwers.InvalidArgument(nameof(ctx.MeshCount));
        if(ctx.TotalVertexCount > 0 || ctx.TotalFaceCount > 0) 
            Throwers.InvalidArgument("Mesh context is not empty.");
        
        var numMeshes = ctx.MeshCount;
        for (var i = 0; i < numMeshes; i++)
        {
            var meshIndex = (byte)i;

            var aiMesh = scene->MMeshes[meshIndex];
            int vertCount = (int)aiMesh->MNumVertices, faceCount = (int)aiMesh->MNumFaces;

            ctx.TotalVertexCount += vertCount;
            ctx.TotalFaceCount += faceCount;

            var materialIndex = (byte)aiMesh->MMaterialIndex;
            var info = new MeshInfo(vertCount, faceCount, meshIndex, materialIndex, (ushort)aiMesh->MNumBones);
            ctx.Meshes[meshIndex] = new MeshEntry(aiMesh->MName.AsString, info);
        }

    }

    //

    private static void TraverseScene(AssimpNode* node, ModelImportContext ctx, in Matrix4x4 parentWorld)
    {
        if (node == null) return;

        var world = node->MTransformation * parentWorld;

        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var meshIndex = (int)node->MMeshes[i];
            ctx.MeshContext.Meshes[meshIndex].SetTransform(in world);
        }

        for (var i = 0; i < node->MNumChildren; i++)
        {
            if (node->MChildren[i] == null) continue;
            TraverseScene(node->MChildren[i], ctx, in world);
        }

        if (ctx.IsAnimated && ctx.TryGetBoneIndex(GetNameHash(node->MName), out var boneIndex))
        {
            ctx.AnimationContext.BindPose[boneIndex] = node->MTransformation;

            var parent = node->MParent;
            if (parent != null && ctx.TryGetBoneIndex(GetNameHash(parent->MName), out var parentIdx))
                ctx.AnimationContext.ParentIndices[boneIndex] = (byte)parentIdx;
            else
                ctx.AnimationContext.ParentIndices[boneIndex] = 0;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ProcessMeshVertices(AssimpMesh* aiMesh, int meshIndex, ModelImportContext ctx)
    {
        var is16Bit = ctx.MeshContext.GetMeshData(meshIndex, out var vertices, out var skinned, out var indices);

        if (is16Bit)
            WriteIndicesU16(aiMesh, indices.Reinterpret<ushort>());
        else
            WriteIndicesU32(aiMesh, indices.Reinterpret<uint>());

        WriteVertices(aiMesh, meshIndex, ctx.MeshContext, vertices);

        if (ctx.IsAnimated)
            WriteSkinningData(aiMesh, ctx, skinned);
    }
}