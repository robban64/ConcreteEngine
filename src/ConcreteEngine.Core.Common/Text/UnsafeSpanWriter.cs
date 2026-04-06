using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Common.Text;

public unsafe struct UnsafeSpanWriter(byte* buffer, int capacity)
{
    public UnsafeSpanWriter(NativeViewPtr<byte> buffer) : this(buffer, buffer.Length) { }

    public readonly byte* Buffer = buffer;
    public readonly int Capacity = capacity;
    private int _cursor;

    public readonly int Cursor => _cursor;

    public void Clear() => _cursor = 0;
    public void SetCursor(int cursor) => _cursor = cursor;
    public readonly int BytesLeft => Capacity - _cursor;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> AsSpan(int start = 0) => new(Buffer + start, Capacity - start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* End(int index = 0)
    {
        Buffer[_cursor] = 0;
        _cursor = 0;
        return Buffer + index;
    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> EndSpan()
    {
        Buffer[_cursor] = 0;
        var span = new ReadOnlySpan<byte>(Buffer, _cursor);
        _cursor = 0;
        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* Write(char value)
    {
        var written = UtfText.FormatChar(ref *Buffer, value);
        Buffer[written] = 0;
        return Buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* Write(int value)
    {
        var written = UtfText.Format(value, ref *Buffer, Capacity);
        Buffer[written] = 0;
        return Buffer;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* Write(uint value)
    {
        var written = UtfText.Format(value, ref *Buffer, Capacity);
        Buffer[written] = 0;
        return Buffer;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* Write(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        {
            Buffer[0] = 0;
            return Buffer;
        }

        Unsafe.CopyBlockUnaligned(ref *Buffer, ref MemoryMarshal.GetReference(value), (uint)value.Length);
        Buffer[value.Length] = 0;
        return Buffer;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* Write(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            Buffer[0] = 0;
            return Buffer;
        }

        var dest = MemoryMarshal.CreateSpan(ref *Buffer, Capacity - 1);
        Utf8.FromUtf16(value, dest, out _, out var written);
        Buffer[written] = 0;
        return Buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* Write<T>(T value, ReadOnlySpan<char> format = default) where T : IUtf8SpanFormattable
    {
        var dest = MemoryMarshal.CreateSpan(ref *Buffer, Capacity - 1);
        value.TryFormat(dest, out var written, format, null);
        Buffer[written] = 0;
        return Buffer;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(byte* value)
    {
        if (value == null) return ref this;
        var index = UtfText.GetNullTerminateIndex(ref *value);
        Unsafe.CopyBlockUnaligned(ref Buffer[_cursor], ref *value, (uint)index);
        _cursor += index;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty) return ref this;
        Unsafe.CopyBlockUnaligned(ref Buffer[_cursor], ref MemoryMarshal.GetReference(value), (uint)value.Length);
        _cursor += value.Length;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty) return ref this;
        var dest = MemoryMarshal.CreateSpan(ref *(Buffer + _cursor), Capacity - _cursor);
        Utf8.FromUtf16(value, dest, out _, out var written);
        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(char value)
    {
        var written = UtfText.FormatChar(ref *(Buffer + _cursor), value);
        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(int value)
    {
        var written = UtfText.Format(value, ref *(Buffer + _cursor), Capacity - _cursor);
        _cursor += written;
        return ref this;
    }
    
    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(uint value)
    {
        var written = UtfText.Format(value, ref *(Buffer + _cursor), Capacity - _cursor);
        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append<T>(T value, ReadOnlySpan<char> format = default) where T : IUtf8SpanFormattable
    {
        value.TryFormat(AsSpan(_cursor), out var written, format, null);
        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter PadRight(int amount, byte value = 0x20)
    {
        int safeAmount = Math.Min(amount, Capacity - _cursor);
        NativeMemory.Fill(Buffer + _cursor, (nuint)safeAmount, value);
        _cursor += amount;
        return ref this;
    }
}
