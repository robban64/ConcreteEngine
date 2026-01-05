using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Textures;

internal sealed class TextureLoaderModule(AssetGfxUploader uploader)
    : AssetTypeLoader<Texture2D, TextureRecord>(uploader)
{
    protected override Texture2D Load(TextureRecord record, LoaderContext ctx)
    {
        if (record.TextureKind == TextureKind.CubeMap)
            return LoadCubeMap(record, ctx);

        var data = TextureLoader.LoadTexture(EnginePath.TexturePath, record, out var meta);
        Uploader.UploadTexture(data.Span, in meta, out var result);
        var texture = new Texture2D
        {
            Id = ctx.Id,
            GId = ctx.GId,
            Name = record.Name,
            ResourceId = result.TextureId,
            Width = result.Width,
            Height = result.Height
        };

        if (record.InMemory)
            texture.SetPixelData(data);

        return texture;
    }

    private Texture2D LoadCubeMap(TextureRecord record, LoaderContext ctx)
    {
        var data = TextureLoader.LoadCubeMap(EnginePath.TexturePath, record, out var meta);
        Uploader.UploadCubeMap(data, in meta, out var result);
        return new Texture2D
        {
            Id = ctx.Id,
            GId = ctx.GId,
            Name = record.Name,
            ResourceId = result.TextureId,
            Width = result.Width,
            Height = result.Height
        };
    }

    protected override Texture2D LoadEmbedded(EmbeddedRecord embedded, LoaderContext ctx)
    {
        var record = (TextureEmbeddedRecord)embedded;
        var data = TextureLoader.LoadEmbeddedTexture(record, out var meta);
        Uploader.UploadTexture(data.Span, in meta, out var result);

        var texture = new Texture2D
        {
            Id = ctx.Id,
            GId = record.GId,
            Name = record.EmbeddedName,
            ResourceId = result.TextureId,
            Width = result.Width,
            Height = result.Height,
            IsCoreAsset = false,
            SlotKind = record.SlotKind
        };

        return texture;
    }

    public override void Teardown() { }
}