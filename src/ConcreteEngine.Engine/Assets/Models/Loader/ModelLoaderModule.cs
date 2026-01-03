using System.Runtime.InteropServices;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Models.Loader;

internal sealed class ModelLoaderModule
{
    private ModelLoader _loader;
    private ModelLoaderState _state;

    public ModelLoaderModule(AssetGfxUploader uploader)
    {
        _state = new ModelLoaderState();
        _loader = new ModelLoader(uploader, _state);
    }

    public Model LoadModel(MeshDescriptor manifest, ref LoadAssetContext ctx)
    {
        var result = _loader.LoadMesh(ctx.Id, manifest.Name, manifest.Filename);
        var args = ctx.GetFileArgs();
        ctx.FileSpecs =
        [
            new AssetFileSpec(args.Id, args.GId, AssetStorageKind.FileSystem, manifest.Name, manifest.Filename, result.FileSize)
        ];
        ctx.EmbeddedSpan = CollectionsMarshal.AsSpan(_state.EmbeddedList);

        result.Animation?.ModelName = manifest.Name;

        return new Model
        {
            Id = ctx.Id,
            GId = ctx.GId,
            Name = manifest.Name,
            MeshParts = result.MeshParts,
            Animation = result.Animation,
            DrawCount = result.DrawCount,
            IsCoreAsset = ctx.IsCore,
            Bounds = result.Bounds
        };
    }

    public void ClearState()
    {
        _state.Clear();
    }

    public void Teardown()
    {
        _state.Clear();
        _loader.Teardown();
        _state = null!;
        _loader = null!;
    }
}