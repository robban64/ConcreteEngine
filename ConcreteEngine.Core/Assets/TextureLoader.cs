using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;
using StbImageSharp;

namespace ConcreteEngine.Core.Assets;

public sealed class TextureLoader : IAssetTypeLoader, IGpuLazyTexturePayloadProvider
{
    private readonly IReadOnlyList<TextureManifestRecord> _records;
    private readonly List<Texture2D> _results = new (16);
    private readonly Dictionary<string, ReadOnlyMemory<byte>> _dataCache = new();

    private int _idx = 0;
    
    public bool HasStarted { get; private set; }
    public bool IsFinished =>  _idx >= _records.Count;

    internal IReadOnlyList<Texture2D> Results => _results;


    public TextureLoader(IReadOnlyList<TextureManifestRecord> records)
    {
        _records = records;
    }

    public void ClearCache()
    {
        _results.Clear();
        _dataCache.Clear();
        _results.TrimExcess();
        _dataCache.TrimExcess();
    }

    public bool TryGet(out int queueIndex, out GpuTexturePayload payload)
    {
        HasStarted = true;
        if (_idx >= _records.Count)
        {
            queueIndex = -1;
            payload = null;
            return false;
        }
        
        var record = _records[_idx];
        var path = Path.Combine(AssetPaths.AssetPath, "textures", record.Filename);
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        ValidateImageResult(image);
        
        if(record.InMemory)
            _dataCache.Add(record.Name,image.Data.AsMemory());

        payload = new GpuTexturePayload(
            PixelData:image.Data.AsMemory(),
            Width: image.Width,
            Height: image.Height,
            Format: record.PixelFormat,
            Preset: record.Preset,
            Anisotropy: record.Anisotropy,
            LodBias: record.LodBias
            
        );
        queueIndex = _idx++;
        return true;

    }

    public void Callback(int queueIndex, in  (TextureId, TextureMeta) result)
    {
        var record = _records[queueIndex];
        var (id, meta) = result;

        var data = record.InMemory ? _dataCache[record.Name] : null;
        var texture = new Texture2D
        {
            Name = record.Name,
            Path = record.Filename,
            ResourceId = id,
            Width = meta.Width,
            Height = meta.Height,
            PixelFormat = meta.Format,
            Preset = record.Preset,
            Anisotropy = record.Anisotropy,
            Data = data
        };
        
        _results.Add(texture);
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