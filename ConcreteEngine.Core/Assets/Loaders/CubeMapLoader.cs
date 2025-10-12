#region

using ConcreteEngine.Core.Assets.Manifest;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using StbImageSharp;

#endregion

namespace ConcreteEngine.Core.Assets.Loaders;

internal sealed record CubeMapPayload(
    ReadOnlyMemory<byte>[] FaceData,
    GfxTextureDescriptor TextureDesc,
    GfxTextureProperties TextureProps
);

internal sealed class CubeMapLoader(IReadOnlyList<CubeMapManifestRecord> records)
    : AssetTypeLoader<CubeMapManifestRecord, CubeMapPayload>(records)
{
    public override CubeMapPayload ProcessResource(CubeMapManifestRecord record, out AssetProcessInfo info)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(record.Textures.Length, 6);

        var faceData = new ReadOnlyMemory<byte>[6];

        for (int i = 0; i < 6; i++)
        {
            var path = Path.Combine(AssetPaths.GetAssetPath(), "cubemaps", record.Textures[i]);
            using var stream = File.OpenRead(path); // StbImageSharp
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            ValidateImageResult(image, record);
            faceData[i] = image.Data;
        }

        var desc = new GfxTextureDescriptor(
            width: record.Width,
            height: record.Height,
            kind: TextureKind.CubeMap,
            format: record.PixelFormat
        );

        var props = new GfxTextureProperties(
            preset: record.Preset,
            anisotropy: TextureAnisotropy.Off,
            lodBias: 0
        );

        info = AssetProcessInfo.MakeDone<CubeMapManifestRecord>();
        return new CubeMapPayload(faceData, desc, props);
    }


    protected override void ClearCache()
    {
    }

    private static void ValidateImageResult(ImageResult result, CubeMapManifestRecord record)
    {
        ArgumentNullException.ThrowIfNull(result, nameof(result));

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Width, 0, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Height, 0, nameof(result.Height));

        ArgumentOutOfRangeException.ThrowIfNotEqual(result.Width, record.Width, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(result.Height, record.Height, nameof(result.Height));
    }
}