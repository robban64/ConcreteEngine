using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Graphics;

public sealed class MemoryDataBuffer
{
    private const int DefaultCapacity = 1024 * 4;
    
    private byte[] _buffer = [];
    private int _idx = 0;

    public uint ElementSize { get; private set; } = 0;
    public uint ElementCount { get; private set; } = 0;

    public MemoryDataBuffer(int capacity = DefaultCapacity)
    {
        EnsureCapacity(capacity);
    }
    
    public ReadOnlyMemory<byte> AsReadOnlyMemory() => new (_buffer, 0, _idx);
    public ReadOnlySpan<byte> AsReadOnlySpan() => new (_buffer, 0, _idx);

    public void Clear()
    {
        _idx = 0;
        ElementSize = 0;
        ElementCount = 0;
    }

    public void SetData<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        ElementSize = (uint)Unsafe.SizeOf<T>();
        ElementCount = (uint)data.Length;
        
        var raw = MemoryMarshal.AsBytes(data);  
        EnsureCapacity(raw.Length);
        raw.CopyTo(_buffer.AsSpan(0));
        _idx = raw.Length;
    }
/*
    public void SetData(ReadOnlySpan<byte> raw)
    {
        EnsureCapacity(raw.Length);
        raw.CopyTo(_buffer.AsSpan(0));
        _idx = raw.Length;
    }
    */
    private void EnsureCapacity(int newCapacity)
    {
        if (_buffer.Length >= newCapacity) return;
        int cap = _buffer.Length;
        while (cap < newCapacity) cap = cap * 2;
        Array.Resize(ref _buffer, cap);
    }

}