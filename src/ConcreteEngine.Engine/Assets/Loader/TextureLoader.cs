using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader.Importer;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Metadata.Asset;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class TextureLoader(AssetGfxUploader uploader)
    : AssetTypeLoader<Texture2D, TextureRecord>(uploader)
{
    
    public override void Setup()
    {
        IsActive = true;
    }

    public override void Teardown()
    {
        IsActive = false;
    }

    protected override Texture2D Load(TextureRecord record, ref LoaderContext ctx)
    {
        if (record.TextureKind == TextureKind.CubeMap)
            return LoadCubeMap(record, ref ctx);

        var data = TextureImporter.LoadTexture(EnginePath.TexturePath, record, out var meta);
        Uploader.UploadTexture(data.Span, in meta, out var result);
        var texture = new Texture2D
        {
            Id = ctx.Id,
            GId = record.GId,
            Name = record.Name,
            ResourceId = result.TextureId,
            Width = result.Width,
            Height = result.Height
        };

        if (record.InMemory)
            texture.SetPixelData(data);

        return texture;
    }

    private Texture2D LoadCubeMap(TextureRecord record, ref LoaderContext ctx)
    {
        var data = TextureImporter.LoadCubeMap(EnginePath.TexturePath, record, out var meta);
        Uploader.UploadCubeMap(data, in meta, out var result);
        return new Texture2D
        {
            Id = ctx.Id,
            GId = record.GId,
            Name = record.Name,
            ResourceId = result.TextureId,
            Width = result.Width,
            Height = result.Height
        };
    }

    public Texture2D LoadEmbedded(AssetId assetId, TextureEmbeddedRecord embedded)
    {
        var data = TextureImporter.LoadEmbeddedTexture(embedded, out var meta);
        Uploader.UploadTexture(data.Span, in meta, out var result);

        var texture = new Texture2D
        {
            Id = assetId,
            GId = embedded.GId,
            Name = embedded.AssetName,
            ResourceId = result.TextureId,
            Width = result.Width,
            Height = result.Height,
            IsCoreAsset = false,
            SlotKind = embedded.SlotKind
        };

        return texture;
    }

}