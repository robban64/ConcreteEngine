using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Handles;
using Silk.NET.Assimp;
using static ConcreteEngine.Engine.Assets.Loader.ImporterAssimp.AssimpUtils;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpNode = Silk.NET.Assimp.Node;
using AssimpScene = Silk.NET.Assimp.Scene;

namespace ConcreteEngine.Engine.Assets.Loader.ImporterAssimp;

internal sealed unsafe partial class ModelImporter : IDisposable
{
    private static uint[] _hashes = null!;
    private static short[] _boneIndices = null!;
    private static IntPtr[] _nodes = null!;
    private static int _hashIndex;

    private readonly Dictionary<string, int> _boneIndexByName = new(BoneLimit);

    private Assimp _assimp;
    private AssimpScene* _scene;
    private AssimpSceneMeta _sceneMeta;

    internal ModelImporter()
    {
        _assimp = Assimp.GetApi();
        _hashes = new uint[BoneLimit * 2];
        _boneIndices = new short[BoneLimit * 2];
        _nodes = new IntPtr[BoneLimit * 2];
        _hashIndex = 0;
    }

    private static bool TryGetBoneIndex(uint hash, out int boneIndex)
    {
        var idx = _hashes.IndexOf(hash);
        boneIndex = idx >= 0 ? _boneIndices[idx] : -1;
        return boneIndex >= 0;
    }

    private static bool TryGetNode(uint hash, out IntPtr nodePtr)
    {
        var idx = _hashes.IndexOf(hash);
        nodePtr = idx >= 0 ? _nodes[idx] : -1;
        return nodePtr > 0;
    }


    public void Dispose()
    {
        _hashes = null!;
        _boneIndices = null!;
        _nodes = null!;
        _boneIndexByName.Clear();
        _boneIndexByName.TrimExcess();

        _assimp.Dispose();
        _assimp = null!;
        _scene = null;
    }

    public void Cleanup()
    {
        _sceneMeta = default;
        _boneIndexByName.Clear();
        Array.Clear(_hashes);
        Array.Clear(_nodes);
        _boneIndices.AsSpan().Fill(-2);
        _hashIndex = 0;
        
        _assimp.FreeScene(_scene);
        _scene = null;
    }

    private AssimpScene* LoadScene(string path, string filename)
    {
        var buffer = stackalloc char[1024];
        var ptr = (byte*)buffer;
        var chars = new Span<char>(buffer + 512, 512);

        Path.TryJoin(path, filename, chars, out int written);
        Encoding.UTF8.GetBytes(chars.Slice(0, written), new Span<byte>(ptr, 1024));
        ptr[1024] = 0;
        return _assimp.ImportFile(ptr, (uint)AssimpFlags);
    }

    public ModelImportContext StartImport(string name, string path, string filename)
    {
        if (_hashes.Length == 0 || _boneIndices.Length == 0 || _nodes.Length == 0 || _boneIndexByName.Count > 0)
            throw new InvalidOperationException();

        Cleanup();

        var scene = LoadScene(path, filename);
        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new InvalidOperationException(error);
        }

        if ((int)scene->MNumMeshes == 0)
            throw new InvalidOperationException($"Model {name} contains no meshes");

        _scene = scene;

        PreProcessScene(scene);

        var boneCount = RegisterBones(scene);
        _sceneMeta.FromScene(scene, boneCount);

        var model = RegisterMeshes(scene);
        var animation = RegisterAnimation(scene);
        return new ModelImportContext(name, path, model, animation, _sceneMeta.MaterialCount, _sceneMeta.TextureCount)
        {
            Model = RegisterMeshes(scene), Animation = RegisterAnimation(scene)
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ImportSceneData(ModelImportContext ctx)
    {
        var scene = _scene;
        if (scene == null) throw new InvalidOperationException("Scene cannot be null.");

        TraverseScene(scene->MRootNode, ctx, Matrix4x4.Identity);

        var meta = _sceneMeta;
        for (var i = 0; i < meta.MeshCount; i++)
            ProcessMeshVertices(scene->MMeshes[i], i, ctx);

        if (ctx.Animation is { } animation)
        {
            var sceneAnimations = scene->MAnimations;
            for (var i = 0; i < meta.AnimationCount; i++)
                ProcessAnimation(sceneAnimations[i], animation);
        }

        ProcessMaterials(scene, ctx, meta);
    }
    
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Upload(ModelImportContext ctx, AssetGfxUploader gfxUploader)
    {
        var model = ctx.Model;
        var animation = ctx.Animation;
        var meshes = model.Meshes;
        foreach (var mesh in meshes)
        {
            ctx.Model.GetMeshData(mesh.Info.MeshIndex, out var vertices, out var skinned, out var indices);
            var meshId = animation != null
                ? gfxUploader.UploadAnimatedMesh(vertices, skinned, indices)
                : gfxUploader.UploadMesh(vertices, indices);

            if (!meshId.IsValid())
                throw new InvalidOperationException("Upload returned invalid MeshId");

            mesh.MeshId = meshId;
        }

        var bounds = meshes[0].LocalBounds;
        for (var i = 1; i < meshes.Length; i++)
            BoundingBox.Merge(in bounds, in meshes[i].LocalBounds, out bounds);

        model.ModelBounds = bounds;
        
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private void PreProcessScene(AssimpScene* scene)
    {
        if (_assimp is null) throw new InvalidOperationException(nameof(_assimp));
        _assimp.TransposeMatrix4(&scene->MRootNode->MTransformation);
        TraverseTranspose(_assimp, scene->MRootNode);

        return;

        [MethodImpl(MethodImplOptions.NoInlining)]
        void TraverseTranspose(Assimp assimp, AssimpNode* currentNode)
        {
            for (var i = 0; i < currentNode->MNumChildren; i++)
            {
                var node = currentNode->MChildren[i];
                _hashes[_hashIndex] = GetNameHash(node->MName);
                _nodes[_hashIndex++] = (IntPtr)node;
                assimp.TransposeMatrix4(&node->MTransformation);
                TraverseTranspose(assimp, node);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int RegisterBones(AssimpScene* scene)
    {
        var numMeshes = (int)scene->MNumMeshes;
        var assimp = _assimp;
        for (var i = 0; i < numMeshes; i++)
        {
            var aiMesh = scene->MMeshes[i];
            var boneCount = aiMesh->MNumBones;
            for (var b = 0; b < boneCount; b++)
            {
                var bone = aiMesh->MBones[b];
                assimp.TransposeMatrix4(&bone->MOffsetMatrix);
                RegisterBoneRecursive(bone->MName);
            }
        }

        return _boneIndexByName.Count;

        [MethodImpl(MethodImplOptions.NoInlining)]
        void RegisterBoneRecursive(AssimpString name)
        {
            var hash = GetNameHash(name);

            if (TryGetBoneIndex(hash, out _)) return;
            if (TryGetNode(hash, out var nodePtr))
            {
                var node = (AssimpNode*)nodePtr;
                if (node->MParent != null)
                    RegisterBoneRecursive(node->MParent->MName);
            }

            var hashIndex = _hashes.IndexOf(hash);
            var boneIndex = _boneIndexByName.Count;

            if (hashIndex < 0)
            {
                _hashes[_hashIndex] = hash;
                hashIndex = _hashIndex++;
            }

            _boneIndices[hashIndex] = (short)boneIndex;
            _boneIndexByName[name.AsString] = boneIndex;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ModelImportData RegisterMeshes(AssimpScene* scene)
    {
        var numMeshes = (int)scene->MNumMeshes;

        var model = new ModelImportData(numMeshes);
        for (var i = 0; i < numMeshes; i++)
        {
            var meshIndex = (byte)i;

            var aiMesh = scene->MMeshes[meshIndex];
            int vertCount = (int)aiMesh->MNumVertices, faceCount = (int)aiMesh->MNumFaces;

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
            ctx.Model.Meshes[meshIndex].WorldTransform = world;
        }

        for (var i = 0; i < node->MNumChildren; i++)
        {
            if (node->MChildren[i] == null) continue;
            TraverseScene(node->MChildren[i], ctx, in world);
        }

        if (ctx.Animation is { } animation && TryGetBoneIndex(GetNameHash(node->MName), out var boneIndex))
        {
            animation.Skeleton.BindPose[boneIndex] = local;
            //Matrix4x4.Invert(local, out skeleton.InverseBindPose[boneIndex]); // OffsetMatrix

            var parent = node->MParent;
            if (parent != null && TryGetBoneIndex(GetNameHash(parent->MName), out var parentIdx))
                animation.Skeleton.ParentIndices[boneIndex] = parentIdx;
            else
                animation.Skeleton.ParentIndices[boneIndex] = -1;
            //skeleton.InverseBindPose[boneIndex] = local;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ProcessMeshVertices(AssimpMesh* aiMesh, int meshIndex, ModelImportContext ctx)
    {
        if (ctx.Animation == null)
        {
            ctx.Model.GetMeshData(meshIndex, out var vertices, out _, out var indices);
            WriteIndices(aiMesh, indices);
            WriteVertices(aiMesh, meshIndex, ctx.Model, vertices);
        }
        else
        {
            ctx.Model.GetMeshData(meshIndex, out var vertices, out var skinned, out var indices);
            WriteIndices(aiMesh, indices);
            WriteSkinningData(aiMesh, ctx.Animation, skinned);
            WriteVerticesSkinned(aiMesh, meshIndex, ctx.Model, vertices);
        }
    }

}