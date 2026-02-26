using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader.ImporterAssimp;
using ConcreteEngine.Engine.Configuration.IO;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class ModelLoader(AssetGfxUploader uploader) : AssetTypeLoader<Model, ModelRecord>(uploader)
{
    private ModelImporter? _importer;

    protected override Model Load(ModelRecord record,  LoaderContext ctx)
    {
        if(_importer == null) throw new InvalidOperationException("ModelImport is null");

        if (EmbeddedAssets.Count > 0) throw new InvalidOperationException("EmbeddedAssets is not empty");

        var path = Path.Combine(EnginePath.ModelPath, record.Files.First().Value);
        var fi = new FileInfo(path);
        if (!fi.Exists) throw new FileNotFoundException("File not found.", path);

        var modelContext = _importer.ImportModel(record.Name, path, Uploader);

        var model = modelContext.Model;
        var animation = modelContext.Animation;

        var meshLength = (byte)model.Meshes.Length;
        if(meshLength == 0) throw new InvalidOperationException("Model import resulted in zero meshes");

        byte textureLen = (byte)modelContext.Textures.Count,  materialLen = (byte)modelContext.Materials.Count;

        if (textureLen > 0)
        {
            modelContext.Textures.Sort(static (it1, it2) => it1.TextureIndex.CompareTo(it2.TextureIndex));
            EmbeddedAssets.AddRange(modelContext.Textures);
        }

        if (materialLen > 0)
        {
            modelContext.Materials.Sort(static (it1, it2) => it1.MaterialIndex.CompareTo(it2.MaterialIndex));
            EmbeddedAssets.AddRange(modelContext.Materials);
        }

        var modelInfo = new ModelInfo(
            model.TotalVertexCount,
            model.TotalFaceCount,
            (ushort)(animation?.BoneCount ?? 0),
            meshLength,
            materialLen,
            textureLen,
            animation != null);


        _importer.Cleanup();
        modelContext.Clear();


        return new Model(
            record.Name,
            modelInfo,
            in model.ModelBounds,
            model.Meshes,
            model.WorldTransforms,
            animation) { Id = ctx.Id, GId = record.GId };
    }

    public override void Setup()
    {
        IsActive = true;
        EmbeddedAssets.EnsureCapacity(16);
        _importer = new ModelImporter(Uploader.GetMeshScratchpad());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Teardown()
    {
        EmbeddedAssets.Clear();
        _importer?.Dispose();
        _importer = null;
        IsActive = false;
    }
}