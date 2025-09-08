using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;
using StbImageSharp;

namespace ConcreteEngine.Core.Assets;

public sealed class CubeMapLoader : IAssetTypeLoader, IGpuLazyCubeMapPayloadProvider
{
    private readonly IReadOnlyList<CubeMapManifestRecord> _records;
    private readonly List<CubeMap> _results = new(16);

    private int _idx = 0;
    
    public bool HasStarted { get; private set; }
    public bool IsFinished =>  _idx >= _records.Count;


    internal IReadOnlyList<CubeMap> Results => _results;

    public CubeMapLoader(IReadOnlyList<CubeMapManifestRecord> records)
    {
        _records = records;
    }

    public void ClearCache()
    {
        _results.Clear();
        _results.TrimExcess();
    }

    public bool TryGet(out int queueIndex, out GpuCubeMapPayload payload)
    {
        HasStarted = true;
        if (_idx >= _records.Count)
        {
            queueIndex = -1;
            payload = null;
            return false;
        }
        var record = _records[_idx];
        ArgumentOutOfRangeException.ThrowIfNotEqual(record.Textures.Length, 6, nameof(record.Textures));

        var pixelData = new ReadOnlyMemory<byte>[6];

        int width = -1, height = -1;
        for (int i = 0; i < 6; i++)
        {
            var image = GetImage(record, i);
            if (width > 0 && height > 0 && (image.Width != width || image.Height != height))
            {
                throw new InvalidOperationException($"Texture size mismatch {width}x{height} {image.Width}x{image.Height}");
            }
            width = image.Width;
            height = image.Height;
            pixelData[i] = image.Data.AsMemory();
        }

        payload = new GpuCubeMapPayload(
            FaceData: pixelData,
            Width: width,
            Height: height,
            Format: record.PixelFormat
        );
        queueIndex = _idx++;
        return true;

    }
    
    public void Callback(int queueIndex, in (TextureId, TextureMeta) result)
    {
        var record = _records[queueIndex];
        var (id, meta) = result;

        var cubemap = new CubeMap
        {
            Name = record.Name,
            ResourceId = id,
            Width = meta.Width,
            Height = meta.Height,
            PixelFormat = meta.Format,
            Textures = record.Textures
        };

        _results.Add(cubemap);
    }

    private ImageResult GetImage(CubeMapManifestRecord record, int faceIndex)
    {
        var path = Path.Combine(AssetPaths.AssetPath, "cubemaps", record.Textures[faceIndex]);
        using var stream = File.OpenRead(path);
        var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        ValidateImageResult(result);
        return result;
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