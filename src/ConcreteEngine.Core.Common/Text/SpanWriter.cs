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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<char> WrittenSpan() => _buffer.Slice(0, _cursor + 1);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<char> LeftSpan() => _buffer.Slice(_cursor);


    [MethodImpl(MethodImplOptions.AggressiveInlining), UnscopedRef]
    public ref SpanWriter Append(scoped ReadOnlySpan<char> value)
    {
        if (value.Length == 0) return ref this;

        var dst = _buffer.Slice(_cursor);
        if (value.Length > dst.Length) ThrowBufferTooSmall();

        value.CopyTo(dst);
        _cursor += value.Length;
        return ref this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), UnscopedRef]
    public ref SpanWriter Append(scoped ReadOnlySpan<byte> value)
    {
        if (value.Length == 0) return ref this;

        if (!Encoding.UTF8.TryGetChars(value, LeftSpan(), out int written))
            ThrowBufferTooSmall();

        _cursor += written;
        return ref this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), UnscopedRef]
    public ref SpanWriter Append<T>(T value, ReadOnlySpan<char> format = default) where T : ISpanFormattable
    {
        if (!value.TryFormat(LeftSpan(), out var written, format, null))
            ThrowBufferTooSmall();

        _cursor += written;
        return ref this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), UnscopedRef]
    public ref SpanWriter PadRight(int amount)
    {
        if (amount <= 0) return ref this;
        var dst = LeftSpan();
        var len = _cursor + amount;
        if ((uint)len > (uint)dst.Length) ThrowBufferFull();

        dst.Slice(0, len).Fill(' ');
        _cursor += len;
        return ref this;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> End()
    {
        if ((uint)_cursor > (uint)_buffer.Length) ThrowBufferFull();
        var span = _buffer.Slice(0, _cursor);
        _cursor = 0;
        return span;
    }


    [MethodImpl(MethodImplOptions.NoInlining), StackTraceHidden, DoesNotReturn]
    private static void ThrowBufferFull() => throw new ArgumentException("Buffer full, cannot terminate.");

    [MethodImpl(MethodImplOptions.NoInlining), StackTraceHidden, DoesNotReturn]
    private static void ThrowBufferTooSmall() => throw new ArgumentException("Buffer too small for write.");
}