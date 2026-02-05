using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Common.Text;

public unsafe struct UnsafeSpanWriter(in NativeArray<byte> buffer)
{
    private int _cursor;
    private readonly NativeArray<byte> _buffer = buffer;

    public void Clear() => _cursor = 0;
    public void SetCursor(int cursor) => _cursor = cursor;

    public readonly byte* At(int index = 0) => _buffer.Ptr + index;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte End(int index = 0)
    {
        _buffer[_cursor] = 0;
        _cursor = 0;
        return ref _buffer.GetRef(index);
    }
    
    public ReadOnlySpan<byte> EndSpan(int index = 0)
    {
        _buffer[_cursor] = 0;
        _cursor = 0;
        return  _buffer.AsSpan(index);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref byte Write(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            _buffer[0] = 0;
            return ref _buffer.GetRef();
        }

        var ptr = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(value));
        var written = Encoding.UTF8.GetBytes(ptr, value.Length, _buffer, _buffer.Capacity - 1);
        _buffer[written] = 0;
        return ref _buffer.GetRef();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref byte Write<T>(T value, ReadOnlySpan<char> format = default) where T : IUtf8SpanFormattable
    {
        value.TryFormat(_buffer.AsSpan(), out var written, format, null);
        _buffer[written] = 0;
        return ref _buffer.GetRef();
    }

    public readonly ref byte Write(ushort value) => ref Write((int)value);
    public readonly ref byte Write(short value) => ref Write((int)value);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref byte Write(int value)
    {
        var written = UtfText.Format(value, _buffer.Ptr, _buffer.Capacity);
        _buffer[written] = 0;
        return ref _buffer.GetRef();
    }


    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty) return ref this;

        ref var src = ref MemoryMarshal.GetReference(value);
        ref var dst = ref _buffer.GetRef(_cursor);

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

        var written = Encoding.UTF8.GetBytes(ptr, value.Length, _buffer.Ptr + _cursor, _buffer.Capacity - _cursor);
        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(int value)
    {
        var written = UtfText.Format(value, _buffer.Ptr + _cursor, _buffer.Capacity - _cursor);
        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append<T>(T value, ReadOnlySpan<char> format = default) where T : IUtf8SpanFormattable
    {
        var dst = _buffer.AsSpan(_cursor);
        value.TryFormat(dst, out var written, format, null);
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