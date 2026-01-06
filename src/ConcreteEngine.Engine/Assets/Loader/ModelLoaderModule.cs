using System.Runtime.InteropServices;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Models.Loader;

internal sealed class ModelLoaderModule : AssetTypeLoader<Model, ModelRecord>
{
    private ModelLoader _loader;
    private ModelLoaderState _state;

    public ModelLoaderModule(AssetGfxUploader uploader) : base(uploader)
    {
        _state = new ModelLoaderState();
        _loader = new ModelLoader(uploader, _state);
    }

    protected override Model Load(ModelRecord record, LoaderContext ctx)
    {
        var result = _loader.LoadMesh(ctx.Id, record.Name, record.Files.First().Value);
        if (_state.EmbeddedList.Count > 0) ctx.Embedded = new List<EmbeddedRecord>(_state.EmbeddedList);

        result.Animation?.ModelName = record.Name;

        return new Model
        {
            Id = ctx.Id,
            GId = ctx.GId,
            Name = record.Name,
            MeshParts = result.MeshParts,
            Animation = result.Animation,
            DrawCount = result.DrawCount,
            Bounds = result.Bounds
        };
    }

    protected override Model LoadEmbedded(EmbeddedRecord embedded, LoaderContext context) =>
        throw new NotImplementedException();

    public override void Setup()
    {
        IsActive = true;
    }

    public override void Teardown()
    {
        _state.Clear();
        _loader.Teardown();
        _state = null!;
        _loader = null!;
        IsActive = false;
    }
}