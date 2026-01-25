using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Editor.Utils;

public unsafe ref struct StrWriter8(NativeArray<byte> buffer)
{
    private int _cursor;
    public void Clear() => _cursor = 0;

    public readonly StrWriter8 GetSlicedWriter(int start)
    {
        return new StrWriter8(buffer.Slice(start, buffer.Capacity - start));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref byte Write(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            buffer[0] = 0;
            return ref buffer.GetRef();
        }

        var ptr = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(value));
        var written = Encoding.UTF8.GetBytes(ptr, value.Length, buffer, buffer.Capacity - 1);
        buffer[written] = 0;
        return ref buffer.GetRef();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref byte Write<T>(T value, ReadOnlySpan<char> format = default)
        where T : IUtf8SpanFormattable
    {
        value.TryFormat(buffer.AsSpan(), out var written, format, null);
        buffer[written] = 0;
        return ref buffer.GetRef();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte End()
    {
        buffer[_cursor] = 0;
        _cursor = 0;
        return ref buffer.GetRef();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AppendInternal(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty) return;

        ref var src = ref MemoryMarshal.GetReference(value);
        ref var dst = ref buffer.GetRef(_cursor);

        Unsafe.CopyBlockUnaligned(ref dst, ref src, (uint)value.Length);
        _cursor += value.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AppendInternal(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty) return;

        ref var src = ref MemoryMarshal.GetReference(value);
        var ptr = (char*)Unsafe.AsPointer(ref src);

        var written = Encoding.UTF8.GetBytes(ptr, value.Length, buffer.Ptr + _cursor, buffer.Capacity - _cursor);
        _cursor += written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AppendInternal<T>(T value, ReadOnlySpan<char> format = default) where T : IUtf8SpanFormattable
    {
        var dst = buffer.AsSpan(_cursor);
        value.TryFormat(dst, out var written, format, null);
        _cursor += written;
    }
}

public static class UnsafeSpanWriterExtension
{
    extension(ref StrWriter8 sw)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref StrWriter8 Start(ReadOnlySpan<byte> value)
        {
            sw.Clear();
            sw.AppendInternal(value);
            return ref sw;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref StrWriter8 Start(ReadOnlySpan<char> value)
        {
            sw.Clear();
            sw.AppendInternal(value);
            return ref sw;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref StrWriter8 Start<T>(T value, ReadOnlySpan<char> format = default)
            where T : IUtf8SpanFormattable
        {
            sw.Clear();
            sw.AppendInternal(value, format);
            return ref sw;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref StrWriter8 Append(ReadOnlySpan<byte> value)
        {
            sw.AppendInternal(value);
            return ref sw;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref StrWriter8 Append(ReadOnlySpan<char> value)
        {
            sw.AppendInternal(value);
            return ref sw;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref StrWriter8 Append<T>(T value, ReadOnlySpan<char> format = default)
            where T : IUtf8SpanFormattable
        {
            sw.AppendInternal(value, format);
            return ref sw;
        }
    }
}