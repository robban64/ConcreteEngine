using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader.Importer;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class TextureLoader(AssetGfxUploader uploader)
    : AssetTypeLoader<Texture, TextureRecord>(uploader)
{
    public override void Setup()
    {
        IsActive = true;
    }

    public override void Teardown()
    {
        IsActive = false;
    }

    protected override Texture Load(TextureRecord record, ref LoaderContext ctx)
    {
        if (record.TextureKind == TextureKind.CubeMap)
            return LoadCubeMap(record, ref ctx);

        var data = TextureImporter.LoadTexture(EnginePath.TexturePath, record, out var meta);
        Uploader.UploadTexture(data.Span, in meta, out var result);
        var texture = new Texture
        {
            Id = ctx.Id,
            GId = record.GId,
            Name = record.Name,
            GfxId = result.TextureId,
            Width = result.Width,
            Height = result.Height
        };

        if (record.InMemory)
            texture.SetPixelData(data);

        return texture;
    }

    private Texture LoadCubeMap(TextureRecord record, ref LoaderContext ctx)
    {
        var data = TextureImporter.LoadCubeMap(EnginePath.TexturePath, record, out var meta);
        Uploader.UploadCubeMap(data, in meta, out var result);
        return new Texture
        {
            Id = ctx.Id,
            GId = record.GId,
            Name = record.Name,
            GfxId = result.TextureId,
            Width = result.Width,
            Height = result.Height
        };
    }

    public Texture LoadEmbedded(AssetId assetId, TextureEmbeddedRecord embedded)
    {
        var data = TextureImporter.LoadEmbeddedTexture(embedded, out var meta);
        Uploader.UploadTexture(data.Span, in meta, out var result);

        var texture = new Texture
        {
            Id = assetId,
            GId = embedded.GId,
            Name = embedded.AssetName,
            GfxId = result.TextureId,
            Width = result.Width,
            Height = result.Height,
            IsCoreAsset = false,
            SlotKind = embedded.SlotKind
        };

        return texture;
    }
}