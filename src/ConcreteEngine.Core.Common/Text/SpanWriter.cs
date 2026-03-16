using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace ConcreteEngine.Core.Common.Text;

public ref struct SpanWriter(Span<char> buffer)
{
    public static SpanWriter Make(Span<char> buffer) => new(buffer);

    //
    private readonly Span<char> _buffer = buffer;
    private int _cursor;
    //

    public readonly int Cursor => _cursor;

    public void Clear() => _cursor = 0;
    public void Advance(int count) => _cursor += count;

    public readonly Span<char> WrittenSpan() => _buffer.Slice(0, _cursor + 1);
    public readonly Span<char> LeftSpan() => _buffer.Slice(_cursor);

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<char> Write(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty) return ReadOnlySpan<char>.Empty;

        var dst = _buffer.Slice(_cursor);
        if (value.Length > dst.Length) ThrowBufferTooSmall();

        value.CopyTo(dst);
        return _buffer.Slice(0, value.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<char> Write<T>(T value, ReadOnlySpan<char> format = default)
        where T : ISpanFormattable
    {
        if (!value.TryFormat(_buffer, out var written, format, null)) ThrowBufferTooSmall();
        return _buffer.Slice(0, written);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining), UnscopedRef]
    public ref SpanWriter Append(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty) return ref this;

        var dst = _buffer.Slice(_cursor);
        if (value.Length > dst.Length) ThrowBufferTooSmall();

        value.CopyTo(dst);
        _cursor += value.Length;
        return ref this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), UnscopedRef]
    public ref SpanWriter Append(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty) return ref this;

        var dst = _buffer.Slice(_cursor);
        if (!Encoding.UTF8.TryGetChars(value, dst, out int written))
            ThrowBufferTooSmall();

        _cursor += written;
        return ref this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), UnscopedRef]
    public ref SpanWriter Append<T>(T value, ReadOnlySpan<char> format = default) where T : ISpanFormattable
    {
        var dst = _buffer.Slice(_cursor);
        if (!value.TryFormat(dst, out var written, format, null))
            ThrowBufferTooSmall();

        _cursor += written;
        return ref this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), UnscopedRef]
    public ref SpanWriter PadRight(int amount)
    {
        if (amount <= 0) return ref this;
        var dst = _buffer.Slice(_cursor);
        var len = _cursor + amount;
        if ((uint)len > dst.Length) ThrowBufferFull();

        dst.Slice(0, len).Fill(' ');
        _cursor += len;
        return ref this;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> End()
    {
        if ((uint)_cursor > _buffer.Length) ThrowBufferFull();
        var span = _buffer.Slice(0, _cursor);
        _cursor = 0;
        return span;
    }


    [MethodImpl(MethodImplOptions.NoInlining), StackTraceHidden, DoesNotReturn]
    private static void ThrowBufferFull() => throw new ArgumentException("Buffer full, cannot terminate.");

    [MethodImpl(MethodImplOptions.NoInlining), StackTraceHidden, DoesNotReturn]
    private static void ThrowBufferTooSmall() => throw new ArgumentException("Buffer too small for write.");
}

internal static class SpanWriterExtensions
{
    extension(ref SpanWriter writer)
    {
        public ref SpanWriter Start(ReadOnlySpan<byte> value)
        {
            writer.Clear();
            return ref writer.Append(value);
        }

        public ref SpanWriter Start(ReadOnlySpan<char> value)
        {
            writer.Clear();
            return ref writer.Append(value);
        }

        public ref SpanWriter Start<T>(T value, ReadOnlySpan<char> format = default) where T : ISpanFormattable
        {
            writer.Clear();
            return ref writer.Append(value, format);
        }

        public ref SpanWriter AppendPadRight(ReadOnlySpan<char> value, int pad)
        {
            writer.Append(value);
            int padLeft = int.Max(0, pad - value.Length);
            return ref writer.PadRight(padLeft);
        }

        public ref SpanWriter AppendPadRight<T>(T value, int pad, ReadOnlySpan<char> format = default)
            where T : ISpanFormattable
        {
            var start = writer.Cursor;
            writer.Append(value, format);
            var written = writer.Cursor - start;
            int padLeft = int.Max(0, pad - written);
            return ref writer.PadRight(padLeft);
        }
    }
}