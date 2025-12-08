#region

using System.Runtime.InteropServices;

#endregion

namespace ConcreteEngine.Common.Collections;

public sealed class MemoryDataBuffer
{
    public const int LowCapacity = 1024 * 4;

    public const int MinSize = 1024;

    private byte[] _buffer = [];
    private int _idx = 0;

    public int Count => _idx;
    public int Capacity => _buffer.Length;


    public MemoryDataBuffer(int capacity)
    {
        EnsureCapacity(capacity);
    }

    public ReadOnlyMemory<byte> AsReadOnlyMemory() => new(_buffer, 0, _idx);
    public ReadOnlySpan<byte> AsReadOnlySpan() => new(_buffer, 0, _idx);


    public void SetData<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        var raw = MemoryMarshal.AsBytes(data);
        EnsureCapacity(raw.Length);
        raw.CopyTo(_buffer.AsSpan(0));
        _idx = raw.Length;
    }

    public void SetData(ReadOnlySpan<byte> raw)
    {
        EnsureCapacity(raw.Length);
        raw.CopyTo(_buffer.AsSpan(0));
        _idx = raw.Length;
    }

    public void ResetCursor()
    {
        _idx = 0;
    }

    public void TrimSize(int newSize = LowCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(newSize, LowCapacity, nameof(newSize));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(newSize, _buffer.Length, nameof(newSize));
        Array.Resize(ref _buffer, newSize);
        _idx = 0;
    }

    public void ClearData()
    {
        _idx = 0;
        _buffer = new byte[] { };
    }

    private void EnsureCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(capacity, 0, nameof(capacity));
        var newCapacity = Math.Min(capacity, LowCapacity);
        if (_buffer.Length >= newCapacity) return;
        if (_buffer.Length == 0)
        {
            var newSize = int.Max(newCapacity, LowCapacity);
            Array.Resize(ref _buffer, newSize);
            return;
        }

        var cap = _buffer.Length;
        while (cap < newCapacity) cap *= 2;
        Array.Resize(ref _buffer, cap);
    }
}