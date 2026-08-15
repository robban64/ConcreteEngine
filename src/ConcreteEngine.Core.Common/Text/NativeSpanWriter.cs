using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Common.Text;

public unsafe ref partial struct NativeSpanWriter
{
    public readonly byte* Buffer;
    public readonly int Capacity;
    private int _cursor;

    public NativeSpanWriter(byte* buffer, int capacity, int cursor = 0)
    {
        Buffer = buffer;
        Capacity = capacity;
        _cursor = cursor;
    }

    public NativeSpanWriter(NativeView<byte> buffer)
    {
        Buffer = buffer;
        Capacity = buffer.Length;
        _cursor = 0;
    }

    public readonly int Cursor => _cursor;
    public readonly int BytesLeft => Capacity - _cursor;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref *Buffer, Capacity - 1);

    public readonly Span<byte> WrittenSpan() => MemoryMarshal.CreateSpan(ref *Buffer, _cursor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> RemainingSpan() => MemoryMarshal.CreateSpan(ref Buffer[_cursor], BytesLeft);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _cursor = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCursor(int cursor)
    {
        if ((uint)cursor >= (uint)Capacity) Throwers.BufferOverflow(nameof(NativeSpanWriter), cursor, Capacity);
        _cursor = cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Validate(int length)
    {
        if ((uint)length + (uint)_cursor >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), length + _cursor, Capacity);
        return length > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> EndSpan() => End().AsSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<byte> End()
    {
        var cursor = _cursor;
        _cursor = 0;
        Buffer[cursor] = 0;
        return new NativeView<byte>(Buffer, cursor);
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter PadRight(int amount, byte value = 0x20)
    {
        var start = _cursor;
        var end = start + int.Clamp(amount, 0, Capacity - start);
        while (start < end)
        {
            Buffer[start] = value;
            ++start;
        }

        _cursor = end;
        return ref this;
    }
}

[InterpolatedStringHandler]
public ref struct NativeSpanWriterHandler(int literalLength, int formattedCount, NativeSpanWriter writer)
{
    private NativeSpanWriter _writer = writer;

    public void AppendLiteral(string s) => _writer.Append(s);

    public void AppendLiteral(ReadOnlySpan<char> s) => _writer.Append(s);
    public void AppendLiteral(ReadOnlySpan<byte> s) => _writer.Append(s);

    public void AppendFormatted<T>(T t) where T : IUtf8SpanFormattable => _writer.Append(t);

    public void AppendFormatted<T>(T t, string? format) where T : IUtf8SpanFormattable => _writer.Append(t, format);

    public void AppendFormatted(string? s)
    {
        if (s is not null) _writer.Append(s);
    }

    public void AppendFormatted<T>(T value, int alignment) where T : IUtf8SpanFormattable
    {
        Span<byte> tmp = stackalloc byte[64];
        if (value.TryFormat(tmp, out int written, default, null))
        {
            int pad = Math.Abs(alignment) - written;
            if (alignment > 0) _writer.PadRight(pad);
            _writer.Append(tmp[..written]);
            if (alignment < 0) _writer.PadRight(pad);
        }
        else
        {
            _writer.Append(value);
        }
    }

    public void AppendFormatted<T>(T value, int alignment, string? format) where T : IUtf8SpanFormattable
    {
        Span<byte> tmp = stackalloc byte[64];
        if (value.TryFormat(tmp, out int written, format, null))
        {
            int pad = Math.Abs(alignment) - written;
            if (alignment > 0) _writer.PadRight(pad);
            _writer.Append(tmp[..written]);
            if (alignment < 0) _writer.PadRight(pad);
        }
        else
        {
            _writer.Append(value);
        }
    }

    public void AppendFormatted(ReadOnlySpan<char> s) => _writer.Append(s);
    public void AppendFormatted(ReadOnlySpan<byte> s) => _writer.Append(s);

    public void AppendFormatted(object? value)
    {
        if (value is not null) _writer.Append(value.ToString()!);
    }

    public void AppendFormatted(object? value, string? format)
    {
        if (value is IFormattable f) _writer.Append(f.ToString(format, null)!);
        else if (value is not null) _writer.Append(value.ToString()!);
    }
}