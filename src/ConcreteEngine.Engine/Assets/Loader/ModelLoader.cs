using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader.ImporterModel;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Graphics;
using ModelImporter = ConcreteEngine.Engine.Assets.Loader.ImporterModel.ModelImporter;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class ModelLoader : AssetTypeLoader<Model, ModelRecord>
{
    private ModelImporter _importer;

    public ModelLoader(AssetGfxUploader uploader) : base(uploader)
    {
        _importer = new ModelImporter();
        EmbeddedAssets.EnsureCapacity(16);
    }

    protected override Model Load(ModelRecord record, ref LoaderContext ctx)
    {
        if (EmbeddedAssets.Count > 0)
            throw new InvalidOperationException("EmbeddedAssets is not empty");

        var path = Path.Combine(EnginePath.ModelPath, record.Files.First().Value);
        var fi = new FileInfo(path);
        if (!fi.Exists) throw new FileNotFoundException("File not found.", path);

        _importer.ImportModel(record.Name, path, Uploader);

        var modelContext = ModelImportContext.Instance;
        var modelMeshes = modelContext.Model;
        var animation = modelContext.Animation;


        modelContext.Textures.Sort(static (it1, it2) => it1.TextureIndex.CompareTo(it2.TextureIndex));
        modelContext.Materials.Sort(static (it1, it2) => it1.MaterialIndex.CompareTo(it2.MaterialIndex));

        if (modelContext.Textures.Count > 0)
            EmbeddedAssets.AddRange(modelContext.Textures);

        if (modelContext.Materials.Count > 0)
            EmbeddedAssets.AddRange(modelContext.Materials);

        modelContext.End();

        return new Model
        {
            Id = ctx.Id,
            GId = record.GId,
            Name = record.Name,
            VertexCount = modelMeshes.TotalVertexCount,
            FaceCount = modelMeshes.TotalFaceCount,
            Bounds = modelMeshes.ModelBounds,
            Meshes = modelMeshes.Meshes,
            WorldTransforms = modelMeshes.WorldTransforms,
            Animation = animation,
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Setup()
    {
        ModelImportContext.CreateContext(Uploader.GetMeshScratchpad());
        IsActive = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Teardown()
    {
        EmbeddedAssets.Clear();
        ModelImportContext.CloseContext();
        _importer.Dispose();
        _importer = null!;
        IsActive = false;
    }
}