using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Loader.ImporterAssimp;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Assets.Loader;

internal readonly ref struct ModelBufferData(
    NativeViewPtr<Vertex3D> vertexPtr,
    NativeViewPtr<SkinningData> skinnedPtr,
    NativeViewPtr<uint> indexPtr)
{
    public void GetVertexData(
        ModelImportContext ctx,
        int index,
        out NativeViewPtr<Vertex3D> vertices,
        out NativeViewPtr<SkinningData> skinned,
        out NativeViewPtr<uint> indices)
    {
        var meshes = ctx.Model.Meshes;
        var mesh = meshes[index];
        int vertOffset = 0, vertLength = mesh.Info.VertexCount;
        int indexOffset = 0, indexLength = mesh.Info.TrisCount * 3;

        for (var i = 0; i < index; i++)
        {
            var it = meshes[i];
            vertOffset += it.Info.VertexCount;
            indexOffset += it.Info.TrisCount * 3;
        }

        vertices = vertexPtr.Slice(vertOffset, vertLength);
        indices = indexPtr.Slice(indexOffset, indexLength);
        if (mesh.Info.BoneCount > 0)
        {
            skinned = skinnedPtr.Slice(vertOffset, vertLength);
        }
        else
            skinned = default;
    }
}

internal sealed class ModelLoader(AssetGfxUploader uploader) : AssetTypeLoader<Model, ModelRecord>(uploader)
{
    private const int DefaultLength = 4096 * 32;

    private static readonly int TotalSize =
        DefaultLength * Unsafe.SizeOf<Vertex3D>() +
        DefaultLength * Unsafe.SizeOf<SkinningData>() +
        DefaultLength * Unsafe.SizeOf<uint>();

    public override int SetupAllocSize => TotalSize;
    public override int DefaultAllocSize => TotalSize;

    private ModelImporter? _importer;

    private ArenaBlockPtr _vertexBlock = null;
    private ArenaBlockPtr _skinnedBlock = null;
    private ArenaBlockPtr _indicesBlock = null;

    protected override void OnSetup()
    {
        _importer = new ModelImporter();

        EmbeddedAssets.EnsureCapacity(16);

        var allocator = Allocator;
        _vertexBlock = allocator.AllocBlock(DefaultLength * Unsafe.SizeOf<Vertex3D>());
        _skinnedBlock = allocator.AllocBlock(DefaultLength * Unsafe.SizeOf<SkinningData>());
        _indicesBlock = allocator.AllocBlock(DefaultLength * Unsafe.SizeOf<uint>());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected override void OnTeardown()
    {
        EmbeddedAssets.Clear();
        _importer?.Dispose();
        _importer = null;
    }


    protected override Model Load(ModelRecord record, LoaderContext ctx)
    {
        if (_importer == null) throw new InvalidOperationException("ModelImport is null");
        if (EmbeddedAssets.Count > 0) throw new InvalidOperationException("EmbeddedAssets is not empty");

        var path = Path.Combine(EnginePath.ModelPath, record.Files.First().Value);
        if (!File.Exists(path)) throw new FileNotFoundException("File not found.", path);

        var modelContext = _importer.StartImport(record.Name, EnginePath.ModelPath, record.Files.First().Value);

        var bufferData = new ModelBufferData(
            _vertexBlock.DataPtr.Reinterpret<Vertex3D>(),
            _skinnedBlock.DataPtr.Reinterpret<SkinningData>(),
            _indicesBlock.DataPtr.Reinterpret<uint>());

        _importer.Execute(modelContext, Uploader, bufferData);
        // write

        // upload

        // store
        var modelData = modelContext.Model;
        var animation = modelContext.Animation;

        var meshLength = (byte)modelData.Meshes.Length;
        if (meshLength == 0) throw new InvalidOperationException("Model import resulted in zero meshes");

        if (animation != null)
        {
            for (int i = 0; i < animation.Clips.Count; i++)
            {
                var clip = animation.Clips[i];
                for (int j = 0; j < clip.Channels.Length; j++)
                {
                    if (clip.Channels[j] == null!)
                        clip.Channels[j] = new AnimationChannel(0, 0);
                }
            }
        }

        byte textureLen = (byte)modelContext.Textures.Count, materialLen = (byte)modelContext.Materials.Count;

        var materialRefs = materialLen > 0 ? new AssetIndexRef[materialLen] : [];
        var textureRefs = textureLen > 0 ? new AssetIndexRef[textureLen] : [];

        if (textureLen > 0)
        {
            modelContext.Textures.Sort(static (it1, it2) => it1.TextureIndex.CompareTo(it2.TextureIndex));
            EmbeddedAssets.AddRange(modelContext.Textures);
            for (var i = 0; i < modelContext.Textures.Count; i++)
            {
                var texEntry = modelContext.Textures[i];
                textureRefs[i] = new AssetIndexRef(texEntry.GId, texEntry.TextureIndex);
            }
        }

        if (materialLen > 0)
        {
            modelContext.Materials.Sort(static (it1, it2) => it1.MaterialIndex.CompareTo(it2.MaterialIndex));
            EmbeddedAssets.AddRange(modelContext.Materials);

            for (var i = 0; i < modelContext.Materials.Count; i++)
            {
                var matEntry = modelContext.Materials[i];
                materialRefs[i] = new AssetIndexRef(matEntry.GId, matEntry.MaterialIndex);
            }
        }

        var modelInfo = new ModelInfo(
            vertexCount: modelData.TotalVertexCount,
            faceCount: modelData.TotalFaceCount,
            boneCount: (ushort)(animation?.BoneCount ?? 0),
            meshCount: meshLength,
            materialCount: materialLen,
            textureCount: textureLen,
            isAnimated: animation != null);

        _importer.Cleanup();
        modelContext.Clear();

        return new Model(
            name: record.Name,
            modelInfo: in modelInfo,
            bounds: in modelData.ModelBounds,
            meshes: modelData.Meshes,
            animation: animation,
            assetRefs: new ModelAssetRefs(materialRefs, textureRefs)
        ) { Id = ctx.Id, GId = record.GId };
    }


/*
    protected override Model Load(ModelRecord record, LoaderContext ctx)
    {
        if (_importer == null) throw new InvalidOperationException("ModelImport is null");

        if (EmbeddedAssets.Count > 0) throw new InvalidOperationException("EmbeddedAssets is not empty");

        var path = Path.Combine(EnginePath.ModelPath, record.Files.First().Value);
        if (!File.Exists(path)) throw new FileNotFoundException("File not found.", path);

        var modelContext = _importer.ImportModel(record.Name, path, Uploader);

        var modelData = modelContext.Model;
        var animation = modelContext.Animation;

        var meshLength = (byte)modelData.Meshes.Length;
        if (meshLength == 0) throw new InvalidOperationException("Model import resulted in zero meshes");

        if (animation != null)
        {
            for (int i = 0; i < animation.Clips.Count; i++)
            {
                var clip = animation.Clips[i];
                for (int j = 0; j < clip.Channels.Length; j++)
                {
                    if (clip.Channels[j] == null!)
                        clip.Channels[j] = new AnimationChannel(0, 0);
                }
            }
        }

        byte textureLen = (byte)modelContext.Textures.Count, materialLen = (byte)modelContext.Materials.Count;

        var materialRefs = materialLen > 0 ? new AssetIndexRef[materialLen] : [];
        var textureRefs = textureLen > 0 ? new AssetIndexRef[textureLen] : [];

        if (textureLen > 0)
        {
            modelContext.Textures.Sort(static (it1, it2) => it1.TextureIndex.CompareTo(it2.TextureIndex));
            EmbeddedAssets.AddRange(modelContext.Textures);
            for (var i = 0; i < modelContext.Textures.Count; i++)
            {
                var texEntry = modelContext.Textures[i];
                textureRefs[i] = new AssetIndexRef(texEntry.GId, texEntry.TextureIndex);
            }
        }

        if (materialLen > 0)
        {
            modelContext.Materials.Sort(static (it1, it2) => it1.MaterialIndex.CompareTo(it2.MaterialIndex));
            EmbeddedAssets.AddRange(modelContext.Materials);

            for (var i = 0; i < modelContext.Materials.Count; i++)
            {
                var matEntry = modelContext.Materials[i];
                materialRefs[i] = new AssetIndexRef(matEntry.GId, matEntry.MaterialIndex);
            }
        }

        var modelInfo = new ModelInfo(
            vertexCount: modelData.TotalVertexCount,
            faceCount: modelData.TotalFaceCount,
            boneCount: (ushort)(animation?.BoneCount ?? 0),
            meshCount: meshLength,
            materialCount: materialLen,
            textureCount: textureLen,
            isAnimated: animation != null);

        _importer.Cleanup();
        modelContext.Clear();

        return new Model(
            name: record.Name,
            modelInfo: in modelInfo,
            bounds: in modelData.ModelBounds,
            meshes: modelData.Meshes,
            animation: animation,
            assetRefs: new ModelAssetRefs(materialRefs, textureRefs)
        ) { Id = ctx.Id, GId = record.GId };
    }
*/
    protected override Model LoadInMemory(ModelRecord record, LoaderContext ctx)
        => throw new NotImplementedException();
}