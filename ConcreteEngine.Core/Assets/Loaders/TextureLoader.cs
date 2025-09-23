using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;
using StbImageSharp;

namespace ConcreteEngine.Core.Assets.Loaders;

internal sealed record TexturePayload(ReadOnlyMemory<byte> Data, GpuTextureDescriptor Descriptor);

internal sealed class TextureLoader(IReadOnlyList<TextureManifestRecord> records)
    : AssetTypeLoader<TextureManifestRecord, TexturePayload>(records)
{
    private readonly Dictionary<string, byte[]> _dataCache = new();
    internal IReadOnlyDictionary<string, byte[]> DataCache => _dataCache;

    public override TexturePayload ProcessResource(TextureManifestRecord record, out AssetProcessInfo info)
    {
        //StbImage.stbi_set_flip_vertically_on_load(1);
        
        var path = Path.Combine(AssetPaths.GetAbsolutePath(), "textures", record.Filename);
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        ValidateImageResult(image);

        if (record.InMemory)
            _dataCache.Add(record.Name, image.Data);

        var desc = new GpuTextureDescriptor(
            Width: image.Width,
            Height: image.Height,
            Format: record.PixelFormat,
            Kind: TextureKind.Texture2D,
            Preset: record.Preset,
            Anisotropy: record.Anisotropy,
            LodBias: record.LodBias
        );

        info = AssetProcessInfo.MakeDone<TextureManifestRecord>();
        return new TexturePayload(image.Data.AsMemory(), desc);    
    }

    protected override void ClearCache()
    {
        _dataCache.Clear();
        _dataCache.TrimExcess();
    }

    private static void ValidateImageResult(ImageResult result)
    {
        ArgumentNullException.ThrowIfNull(result, nameof(result));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Width, 0, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Height, 0, nameof(result.Height));
    }
}