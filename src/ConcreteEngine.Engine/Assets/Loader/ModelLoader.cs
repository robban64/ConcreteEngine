using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Loader.ImporterAssimp;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class ModelLoader(AssetGfxUploader uploader) : AssetTypeLoader<Model, ModelRecord>(uploader)
{
    private const int DefaultLength = 4096 * 32;

    private static readonly int TotalSize =
        DefaultLength * Unsafe.SizeOf<Vertex3D>() +
        DefaultLength * Unsafe.SizeOf<SkinningData>() +
        DefaultLength * Unsafe.SizeOf<uint>() * 3;

    public override int SetupAllocSize => TotalSize;
    public override int DefaultAllocSize => TotalSize;
    
    //

    private ModelImporter? _importer;

    protected override void OnSetup()
    {
        _importer = new ModelImporter();
        EmbeddedAssets.EnsureCapacity(16);
    }

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

        var filename = record.Files.First().Value;

        Allocator.Clear();

        // load scene
        var modelContext = _importer.StartImport(record.Name, EnginePath.ModelPath, filename);
        AllocMeshBlocks(modelContext);

        // write
        _importer.ImportSceneData(modelContext);

        // upload
        _importer.Upload(modelContext, Uploader);

        // store
        var modelData = modelContext.Model;
        var animation = modelContext.Animation;

        var meshLength = (byte)modelData.Meshes.Length;
        if (meshLength == 0) throw new InvalidOperationException("Model import resulted in zero meshes");

        modelContext.SanitizeClips();

        ProcessEmbedded(modelContext, out var materialRefs, out var textureRefs);

        var modelInfo = new ModelInfo(
            vertexCount: modelData.TotalVertexCount,
            faceCount: modelData.TotalFaceCount,
            boneCount: (ushort)(animation?.BoneCount ?? 0),
            meshCount: meshLength,
            materialCount: (byte)materialRefs.Length,
            textureCount: (byte)textureRefs.Length,
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


    private void AllocMeshBlocks(ModelImportContext modelContext)
    {
        var modelImportData = modelContext.Model;
        var allocator = Allocator;
        for (int i = 0; i < modelImportData.Meshes.Length; i++)
        {
            var info = modelImportData.Meshes[i].Info;
            
            modelImportData.Blocks[i] = allocator.Alloc(info.VertexCount * Unsafe.SizeOf<Vertex3D>());
            allocator.Alloc(info.TrisCount * Unsafe.SizeOf<uint>() * 3);
            if (info.BoneCount > 0) allocator.Alloc(info.VertexCount * Unsafe.SizeOf<SkinningData>());
        }
    }

    private void ProcessEmbedded(ModelImportContext modelContext, out AssetIndexRef[] materialRefs,
        out AssetIndexRef[] textureRefs)
    {
        int textureLen = modelContext.Textures.Count, materialLen = modelContext.Materials.Count;

        materialRefs = materialLen > 0 ? new AssetIndexRef[materialLen] : [];
        textureRefs = textureLen > 0 ? new AssetIndexRef[textureLen] : [];

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
    }


    protected override Model LoadInMemory(ModelRecord record, LoaderContext ctx) => throw new NotImplementedException();
}