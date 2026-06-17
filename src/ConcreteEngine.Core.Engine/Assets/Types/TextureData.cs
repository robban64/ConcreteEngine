using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Engine.Assets;

internal sealed class TextureData : IDisposable
{
    public bool IsDisposed { get; private set; }

    public readonly Guid AssetGId;
    private NativeArray<byte> _data;

    public TextureData(Guid assetGId, in NativeArray<byte> data)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(assetGId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(data.IsNull, true, nameof(data));
        AssetGId = assetGId;
        _data = data;
    }
    
    public ReadOnlySpan<byte> GetPixelData()
    {
        if (IsDisposed) throw new ObjectDisposedException(nameof(TextureData));
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
