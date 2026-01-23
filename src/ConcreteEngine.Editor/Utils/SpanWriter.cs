using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace ConcreteEngine.Editor.Utils;

public ref struct SpanWriter(Span<byte> buffer)
{
    private readonly Span<byte> _buffer = buffer;
    private int _cursor;

    public void Clear() => _cursor = 0;
    
    public void Advance(int count) => _cursor += count;

    public readonly Span<byte> WrittenSpan() => _buffer.Slice(0, _cursor + 1);
    public readonly Span<byte> LeftSpan() => _buffer.Slice(_cursor);

    public readonly SpanWriter GetSlicedWriter(int start, int length)
    {
        if((uint)(start+length) > _buffer.Length)
            SpanWriterExtensions.ThrowOverflow();

        return new SpanWriter(_buffer.Slice(start, length));
    }

    public readonly ReadOnlySpan<byte> Write(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty) return ReadOnlySpan<byte>.Empty;
        if (!Encoding.UTF8.TryGetBytes(value, _buffer, out var written))
            SpanWriterExtensions.ThrowOverflow();

        _buffer[written] = 0;
        return _buffer.Slice(0, written + 1);
    }

    public readonly ReadOnlySpan<byte> Write<T>(T value, ReadOnlySpan<char> format = default)
        where T : IUtf8SpanFormattable
    {
        if (!value.TryFormat(_buffer, out var written, format, null))
            SpanWriterExtensions.ThrowOverflow();

        _buffer[written] = 0;
        return _buffer.Slice(0, written + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> End()
    {
        if ((uint)_cursor >= _buffer.Length)
            SpanWriterExtensions.ThrowOverflow();

        _buffer[_cursor] = 0;
        var span = WrittenSpan();
        _cursor = 0;
        return span;
    }

    public void FillChar(byte char8, int length)
    {
        var left = LeftSpan();
        if ((uint)length > left.Length)
            SpanWriterExtensions.ThrowOverflow();

        left.Slice(0, length).Fill(char8);
        Advance(length);
    }
}

internal static class SpanWriterExtensions
{
    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowOverflow() => throw new ArgumentException("Buffer too small.");

    extension(ref SpanWriter writer)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref SpanWriter PadRight(int strLen, int pad, byte char8 = 0x20)
        {
            var padLen = pad - strLen;
            if (padLen <= 0) return ref writer;
            writer.FillChar(char8, padLen);
            return ref writer;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref SpanWriter Start(ReadOnlySpan<byte> value)
        {
            writer.Clear();
            return ref writer.Append(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref SpanWriter Start(ReadOnlySpan<char> value)
        {
            writer.Clear();
            return ref writer.Append(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref SpanWriter Start<T>(T value, ReadOnlySpan<char> format = default) where T : IUtf8SpanFormattable
        {
            writer.Clear();
            return ref writer.Append(value, format);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref SpanWriter Append(ReadOnlySpan<byte> value)
        {
            if (value.IsEmpty) return ref writer;

            var dst = writer.LeftSpan();
            if (value.Length > dst.Length)
                ThrowOverflow();

            value.CopyTo(dst);
            writer.Advance(value.Length);
            return ref writer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref SpanWriter Append(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty) return ref writer;

            var dst = writer.LeftSpan();
            if (!Encoding.UTF8.TryGetBytes(value, dst, out int written))
                ThrowOverflow();

            writer.Advance(written);
            return ref writer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref SpanWriter Append<T>(T value, ReadOnlySpan<char> format = default) where T : IUtf8SpanFormattable
        {
            var dst = writer.LeftSpan();
            if (!value.TryFormat(dst, out var written, format, null))
                ThrowOverflow();

            writer.Advance(written);
            return ref writer;
        }
    }
}