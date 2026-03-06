using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace ConcreteEngine.Core.Common.Text;

public unsafe struct UnsafeSpanWriter(byte* buffer, int capacity)
{
    public byte* Buffer = buffer;
    public readonly int Capacity = capacity;
    private int _cursor;

    public readonly int Cursor => _cursor;

    public void Clear() => _cursor = 0;
    public void SetCursor(int cursor) => _cursor = cursor;
    public int BytesLeft => Capacity - _cursor;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> AsSpan(int start = 0) => new(Buffer + start, Capacity - start);

    public readonly UnsafeSpanWriter GetSlicedWriter(int start, int length) => new(Buffer + start, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* EndPtr(int index = 0)
    {
        Buffer[_cursor] = 0;
        _cursor = 0;
        return Buffer + index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte End(int index = 0)
    {
        Buffer[_cursor] = 0;
        _cursor = 0;
        return ref Buffer[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> EndSpan()
    {
        Buffer[_cursor] = 0;
        var span = AsSpan().Slice(0, _cursor);
        _cursor = 0;
        return span;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* Write(char value)
    {
        var written = UtfText.FormatChar(Buffer, value);
        Buffer[written] = 0;
        return Buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* Write(int value)
    {
        var written = UtfText.Format(value, Buffer, Capacity);
        Buffer[written] = 0;
        return Buffer;
    }

    public readonly byte* Write(ushort value) => Write((int)value);
    public readonly byte* Write(short value) => Write((int)value);


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
    public ref UnsafeSpanWriter Append(ref byte value, int length)
    {
        if (value == 0) return ref this;

        ref var dst = ref Buffer[_cursor];

        Unsafe.CopyBlockUnaligned(ref dst, ref value, (uint)length);
        _cursor += length;
        return ref this;
    }


    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty) return ref this;

        ref var src = ref MemoryMarshal.GetReference(value);
        ref var dst = ref Buffer[_cursor];

        Unsafe.CopyBlockUnaligned(ref dst, ref src, (uint)value.Length);
        _cursor += value.Length;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty) return ref this;

        //ref var src = ref MemoryMarshal.GetReference(value);
        //var ptr = (char*)Unsafe.AsPointer(ref src);
        //var written = Encoding.UTF8.GetBytes(ptr, value.Length, Buffer + _cursor, Capacity - _cursor);

        var dest = MemoryMarshal.CreateSpan(ref *(Buffer + _cursor), Capacity - _cursor);
        Utf8.FromUtf16(value, dest, out _, out var written);

        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(char value)
    {
        var written = UtfText.FormatChar(Buffer + _cursor, value);
        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(int value)
    {
        var written = UtfText.Format(value, Buffer + _cursor, Capacity - _cursor);
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
}

public static class UnsafeSpanWriterExtension
{
    extension(ref UnsafeSpanWriter sw)
    {
        public ref UnsafeSpanWriter Start(ushort value)
        {
            sw.Clear();
            return ref sw.Append((int)value);
        }

        public ref UnsafeSpanWriter Start(ReadOnlySpan<byte> value)
        {
            sw.Clear();
            return ref sw.Append(value);
        }

        public ref UnsafeSpanWriter Start(ReadOnlySpan<char> value)
        {
            sw.Clear();
            return ref sw.Append(value);
        }

        public ref UnsafeSpanWriter Start(char value)
        {
            sw.Clear();
            return ref sw.Append(value);
        }

        public ref UnsafeSpanWriter Start(int value)
        {
            sw.Clear();
            return ref sw.Append(value);
        }

        public ref UnsafeSpanWriter Start<T>(T value, ReadOnlySpan<char> format = default)
            where T : IUtf8SpanFormattable
        {
            sw.Clear();
            return ref sw.Append(value, format);
        }
    }
}