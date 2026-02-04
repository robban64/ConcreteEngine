using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader.Data;
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


    protected override Texture Load(TextureRecord record,  LoaderContext ctx)
    {
        if (record.TextureKind == TextureKind.CubeMap)
            return LoadCubeMap(record,  ctx);

        var data = TextureImporter.LoadTexture(EnginePath.TexturePath, record, out var meta);
        Uploader.UploadTexture(data.Span, in meta, out var result);
        var texture = new Texture
        {
            Id = ctx.Id,
            GId = record.GId,
            Name = record.Name,
            GfxId = result.TextureId,
            Size = new Size2D(result.Width, result.Height),
            LodBias = record.LodBias,
            PixelFormat = record.PixelFormat,
            Anisotropy = record.Anisotropy,
            Preset = record.Preset,
            TextureKind = record.TextureKind
        };

        if (record.InMemory)
            texture.SetPixelData(data);

        return texture;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Texture LoadCubeMap(TextureRecord record,  LoaderContext ctx)
    {
        var data = TextureImporter.LoadCubeMap(EnginePath.TexturePath, record, out var meta);
        Uploader.UploadCubeMap(data, in meta, out var result);
        return new Texture
        {
            Id = ctx.Id,
            GId = record.GId,
            Name = record.Name,
            GfxId = result.TextureId,
            Size = new Size2D(result.Width, result.Height),
            LodBias = record.LodBias,
            PixelFormat = record.PixelFormat,
            Anisotropy = record.Anisotropy,
            Preset = record.Preset,
            TextureKind = record.TextureKind
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Texture LoadEmbedded(AssetId assetId, EmbeddedSceneTexture embedded)
    {
        ArgumentNullException.ThrowIfNull(embedded.Name);
        ArgumentNullException.ThrowIfNull(embedded.PixelData);

        var anisotropy = embedded.SlotKind == TextureUsage.Albedo ? AnisotropyLevel.Default : AnisotropyLevel.Off;
        var meta = TextureImporter.CreateMeta(embedded.Dimensions, embedded.PixelFormat, TextureKind.Texture2D,
            embedded.Preset, TextureImporter.GetAnisotropy(anisotropy), 0);
        
        Uploader.UploadTexture(embedded.PixelData, in meta, out var result);

        var texture = new Texture
        {
            Id = assetId,
            GId = embedded.GId,
            Name = embedded.Name,
            GfxId = result.TextureId,
            Size = new Size2D(result.Width, result.Height),
            LodBias = 0,
            IsCoreAsset = false,
            Usage = embedded.SlotKind,
            PixelFormat = embedded.PixelFormat,
            Preset = embedded.Preset,
            Anisotropy = anisotropy,
            TextureKind = TextureKind.Texture2D
        };
        embedded.PixelData = null!;

        return texture;
    }
}