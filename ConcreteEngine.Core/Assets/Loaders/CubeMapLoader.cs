using ConcreteEngine.Graphics.Descriptors;
using StbImageSharp;

namespace ConcreteEngine.Core.Assets.Loaders;

internal readonly ref struct CubeMapPayload(GpuCubeMapData data, GpuCubeMapDescriptor descriptor)
{
    public readonly GpuCubeMapData Data = data;
    public readonly GpuCubeMapDescriptor Descriptor = descriptor;
}

internal sealed class CubeMapLoader(IReadOnlyList<CubeMapManifestRecord> records)
    : AssetTypeLoader<CubeMapManifestRecord, CubeMapPayload>(records)
{
    public override CubeMapPayload Get(CubeMapManifestRecord record)
    {
        int width = -1, height = -1;
        var image1 = GetImageData(record, 0, ref width, ref height);
        var image2 = GetImageData(record, 1, ref width, ref height);
        var image3 = GetImageData(record, 2, ref width, ref height);
        var image4 = GetImageData(record, 3, ref width, ref height);
        var image5 = GetImageData(record, 4, ref width, ref height);
        var image6 = GetImageData(record, 5, ref width, ref height);

        var data = new GpuCubeMapData(image1.Data.AsSpan(), image2.Data.AsSpan(), image3.Data.AsSpan(),
            image4.Data.AsSpan(), image5.Data.AsSpan(), image6.Data.AsSpan());

        var desc = new GpuCubeMapDescriptor(
            Width: width,
            Height: height,
            Format: record.PixelFormat
        );

        return new CubeMapPayload(data, desc);
    }

    protected override void ClearCache()
    {
    }


    private ImageResult GetImageData(CubeMapManifestRecord record, int faceIndex, ref int width, ref int height)
    {
        var path = Path.Combine(AssetPaths.AssetPath, "cubemaps", record.Textures[faceIndex]);
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        if (width > 0 && height > 0 && (image.Width != width || image.Height != height))
        {
            throw new InvalidOperationException($"Texture size mismatch {width}x{height} {image.Width}x{image.Height}");
        }

        width = image.Width;
        height = image.Height;

        ValidateImageResult(image);
        return image;
    }


    private static void ValidateImageResult(ImageResult result)
    {
        ArgumentNullException.ThrowIfNull(result, nameof(result));
        ArgumentNullException.ThrowIfNull(result.Data, nameof(result.Data));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Data.Length, 0, nameof(result.Data));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Width, 0, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Height, 0, nameof(result.Height));
    }
}