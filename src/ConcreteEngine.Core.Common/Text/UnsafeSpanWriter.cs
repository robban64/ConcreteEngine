using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Common.Text;

public unsafe struct UnsafeSpanWriter(byte* buffer, int capacity)
{
    private byte* _buffer = buffer;
    private int _cursor;

    public UnsafeSpanWriter(in NativeArray<byte> array) : this(array.Ptr, array.Capacity)
    {
    }

    public int Cursor => _cursor;
    public int Capacity => capacity;

    public void Clear() => _cursor = 0;
    public void SetCursor(int cursor) => _cursor = cursor;

    public readonly Span<byte> AsSpan(int start = 0) => new(_buffer + start, capacity - start);

    public readonly UnsafeSpanWriter GetSlicedWriter(int start, int length)
    {
        return new UnsafeSpanWriter(_buffer + start, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte End(int index = 0)
    {
        _buffer[_cursor] = 0;
        _cursor = 0;
        return ref _buffer[index];
    }

    public ReadOnlySpan<byte> EndSpan()
    {
        _buffer[_cursor] = 0;
        var span = AsSpan().Slice(0, _cursor);
        _cursor = 0;
        return span;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref byte Write(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            _buffer[0] = 0;
            return ref _buffer[0];
        }

        var ptr = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(value));
        var written = Encoding.UTF8.GetBytes(ptr, value.Length, _buffer, capacity - 1);
        _buffer[written] = 0;
        return ref _buffer[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref byte Write<T>(T value, ReadOnlySpan<char> format = default) where T : IUtf8SpanFormattable
    {
        value.TryFormat(AsSpan(), out var written, format, null);
        _buffer[written] = 0;
        return ref _buffer[0];
    }

    public readonly ref byte Write(ushort value) => ref Write((int)value);
    public readonly ref byte Write(short value) => ref Write((int)value);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref byte Write(int value)
    {
        var written = UtfText.Format(value, _buffer, capacity);
        _buffer[written] = 0;
        return ref _buffer[0];
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(ref byte value, int length)
    {
        if (value == 0) return ref this;

        ref var dst = ref _buffer[_cursor];

        Unsafe.CopyBlockUnaligned(ref dst, ref value, (uint)length);
        _cursor += length;
        return ref this;
    }


    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty) return ref this;
        
        ref var src = ref MemoryMarshal.GetReference(value);
        ref var dst = ref _buffer[_cursor];

        Unsafe.CopyBlockUnaligned(ref dst, ref src, (uint)value.Length);
        _cursor += value.Length;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty) return ref this;

        ref var src = ref MemoryMarshal.GetReference(value);
        var ptr = (char*)Unsafe.AsPointer(ref src);

        var written = Encoding.UTF8.GetBytes(ptr, value.Length, _buffer + _cursor, capacity - _cursor);
        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(int value)
    {
        var written = UtfText.Format(value, _buffer + _cursor, capacity - _cursor);
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