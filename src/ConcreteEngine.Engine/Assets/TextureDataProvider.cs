using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets;

public static class TextureDataProvider
{
    private static readonly Dictionary<Guid, TextureDataEntry> Entries = new(8);

    public static ReadOnlySpan<byte> GetPixelData(Texture texture) => Entries[texture.GId].GetPixelData();

    internal static void Persist(Texture texture, in NativeArray<byte> data)
    {
        ArgumentNullException.ThrowIfNull(texture);
        var entry = new TextureDataEntry(texture.GId, in data);
        Entries.Add(texture.GId, entry);
    }

    internal static void Free(Texture texture)
    {
        Entries[texture.GId].Dispose();
        Entries.Remove(texture.GId);
    }
    
}

internal sealed class TextureDataEntry : IDisposable
{
    public bool IsDisposed { get; private set; }

    public readonly Guid AssetGId;
    private NativeArray<byte> _data;

    public TextureDataEntry(Guid assetGId, in NativeArray<byte> data)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(assetGId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(data.IsNull, true, nameof(data));
        AssetGId = assetGId;
        _data = data;
    }
    
    public ReadOnlySpan<byte> GetPixelData()
    {
        if (IsDisposed) throw new ObjectDisposedException(nameof(TextureDataEntry));
        if (_data.IsNull) throw new InvalidOperationException("TextureData is not valid");
        
        return _data.AsSpan();
    }

    public void Dispose()
    {
        IsDisposed = true;
        
        _data.Dispose();
        _data = default;
    }
}
