using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader.Importer;
using ConcreteEngine.Engine.Assets.Loader.State;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class ModelLoader : AssetTypeLoader<Model, ModelRecord>
{
    private ModelImporter _importer;
    private ModelLoaderState _state;

    public ModelLoader(AssetGfxUploader uploader) : base(uploader)
    {
        _state = new ModelLoaderState();
        _importer = new ModelImporter(uploader, _state);
    }

    protected override Model Load(ModelRecord record, ref LoaderContext ctx)
    {
        var result = _importer.LoadMesh(ctx.Id, record.Name, record.Files.First().Value);
        if (_state.EmbeddedList.Count > 0) ctx.Embedded = new List<EmbeddedRecord>(_state.EmbeddedList);

        return new Model
        {
            Id = ctx.Id,
            GId = record.GId,
            Name = record.Name,
            MeshParts = result.MeshParts,
            Animation = result.Animation,
            DrawCount = result.DrawCount,
            Bounds = result.Bounds
        };
    }

    public override void Setup()
    {
        IsActive = true;
    }

    public override void Teardown()
    {
        _state.Clear();
        _importer.Teardown();
        _state = null!;
        _importer = null!;
        IsActive = false;
    }
}