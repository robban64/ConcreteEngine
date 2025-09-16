using ConcreteEngine.Graphics.Descriptors;
using StbImageSharp;

namespace ConcreteEngine.Core.Assets.Loaders;

internal readonly ref struct TexturePayload(GpuTextureData data, GpuTextureDescriptor descriptor)
{
    public readonly GpuTextureData Data = data;
    public readonly GpuTextureDescriptor Descriptor = descriptor;
}

internal sealed class TextureLoader(IReadOnlyList<TextureManifestRecord> records) : AssetTypeLoader<TextureManifestRecord, TexturePayload>(records)
{
    private readonly Dictionary<string, byte[]> _dataCache = new();
    internal IReadOnlyDictionary<string, byte[]> DataCache => _dataCache;

    public override TexturePayload Get(TextureManifestRecord record)
    {
        var path = Path.Combine(AssetPaths.GetAbsolutePath(), "textures", record.Filename);
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        ValidateImageResult(image);

        if (record.InMemory)
            _dataCache.Add(record.Name, image.Data);

        var desc = new GpuTextureDescriptor(Width: image.Width,
            Height: image.Height,
            Format: record.PixelFormat,
            Preset: record.Preset,
            Anisotropy: record.Anisotropy,
            LodBias: record.LodBias
        );

        return new TexturePayload(new GpuTextureData(image.Data), desc);
    }

    protected override void ClearCache()
    {
        _dataCache.Clear();
        _dataCache.TrimExcess();
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