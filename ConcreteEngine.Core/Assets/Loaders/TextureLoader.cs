#region

using ConcreteEngine.Core.Assets.Manifest;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Resources;
using StbImageSharp;

#endregion

namespace ConcreteEngine.Core.Assets.Loaders;

internal sealed record TexturePayload(
    byte[] Data,
    GfxTextureDescriptor TextureDesc,
    GfxTextureProperties TextureProps
);

internal sealed class TextureLoader(IReadOnlyList<TextureManifestRecord> records)
    : AssetTypeLoader<TextureManifestRecord, TexturePayload>(records)
{
    public override TexturePayload ProcessResource(TextureManifestRecord record, out AssetProcessInfo info)
    {
        //StbImage.stbi_set_flip_vertically_on_load(1);

        var path = Path.Combine(AssetPaths.GetAbsolutePath(), "textures", record.Filename);
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        ValidateImageResult(image);


        var desc = new GfxTextureDescriptor(
            Width: image.Width,
            Height: image.Height,
            Kind: TextureKind.Texture2D,
            Format: record.PixelFormat
        );

        var props = new GfxTextureProperties(
            Preset: record.Preset,
            Anisotropy: record.Anisotropy,
            LodBias: record.LodBias
        );

        info = AssetProcessInfo.MakeDone<TextureManifestRecord>();
        return new TexturePayload(image.Data, desc, props);
    }


    private static void ValidateImageResult(ImageResult result)
    {
        ArgumentNullException.ThrowIfNull(result, nameof(result));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Width, 0, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Height, 0, nameof(result.Height));
    }
}