using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;
using StbImageSharp;

namespace ConcreteEngine.Core.Assets.Loaders;

internal sealed record CubeMapPayload(ReadOnlyMemory<byte> Data, GpuTextureDescriptor Descriptor, int faceIndex);

internal sealed class CubeMapLoader(IReadOnlyList<CubeMapManifestRecord> records)
    : AssetTypeLoader<CubeMapManifestRecord, CubeMapPayload>(records)
{
    private int faceIdx = 0;

    public override CubeMapPayload ProcessResource(CubeMapManifestRecord record, out AssetProcessInfo info)
    {
        var currentFace = faceIdx;
        var path = Path.Combine(AssetPaths.GetAbsolutePath(), "cubemaps", record.Textures[currentFace]);
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        ValidateImageResult(image);

        ReadOnlyMemory<byte> data = image.Data;
        var desc = new GpuTextureDescriptor(
            Width: image.Width,
            Height: image.Height,
            Format: record.PixelFormat,
            Kind: TextureKind.CubeMap,
            Preset: record.Preset,
            Anisotropy: TextureAnisotropy.Off,
            LodBias: 0
        );
        
        info = currentFace + 1 == 6
            ? AssetProcessInfo.MakeDone<CubeMapManifestRecord>()
            : AssetProcessInfo.MakeRepeat<CubeMapManifestRecord>();

        return new CubeMapPayload(data, desc, currentFace);
    }


    protected override void ClearCache()
    {
    }

    private static void ValidateImageResult(ImageResult result)
    {
        ArgumentNullException.ThrowIfNull(result, nameof(result));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Width, 0, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Height, 0, nameof(result.Height));
    }
}

/*
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
*/