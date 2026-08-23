using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Common.Text;

[InterpolatedStringHandler]
public unsafe ref struct NativeSpanWriter
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

    public readonly int BytesLeft
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Capacity - _cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> AsSpan() => new(Buffer, Capacity - 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> WrittenSpan() => new(Buffer, _cursor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> RemainingSpan() => new(Buffer + _cursor, BytesLeft);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => _cursor = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCursor(int cursor)
    {
        if ((uint)cursor >= (uint)Capacity) Throwers.BufferOverflow(nameof(NativeSpanWriter), cursor, Capacity);
        _cursor = cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<byte> End()
    {
        var cursor = _cursor;
        _cursor = 0;
        Buffer[cursor] = 0;
        return new NativeView<byte>(Buffer, cursor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> EndSpan()
    {
        var cursor = _cursor;
        _cursor = 0;
        Buffer[cursor] = 0;
        return new Span<byte>(Buffer, cursor);
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(byte* value)
    {
        if (value == null) Throwers.NullPointer(nameof(value));
        var src = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(value);
        return ref Append(src);
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(scoped ReadOnlySpan<byte> value)
    {
        Ensure(value.Length);
        value.CopyTo(RemainingSpan());
        _cursor += value.Length;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(scoped ReadOnlySpan<char> value)
    {
        var dst = RemainingSpan();
        if (!Encoding.UTF8.TryGetBytes(value, dst, out var written))
            Throwers.BufferOverflow(nameof(NativeSpanWriter), _cursor + written, Capacity);

        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(byte value)
    {
        Ensure(1);
        Buffer[_cursor] = value;
        _cursor += 1;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(char value)
    {
        Ensure(1);
        _cursor += UtfText.FormatChar(ref Buffer[_cursor], value);
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(int value)
    {
        Ensure(1);
        _cursor += UtfText.Format(value, ref Buffer[_cursor], BytesLeft);
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(uint value)
    {
        Ensure(1);
        _cursor += UtfText.Format(value, ref Buffer[_cursor], BytesLeft);
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append<T>(T value, ReadOnlySpan<char> format = default) where T : IUtf8SpanFormattable
    {
        if (!value.TryFormat(RemainingSpan(), out var written, format, null))
            Throwers.BufferOverflow(nameof(NativeSpanWriter), _cursor + written, Capacity);

        _cursor += written;
        return ref this;
    }


    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter AppendAscii(char c1)
    {
        Ensure(1);
        Buffer[_cursor++] = (byte)c1;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter AppendAscii(char c1, char c2)
    {
        Ensure(2);
        var ptr = Buffer + _cursor;
        *ptr++ = (byte)c1;
        *ptr = (byte)c2;
        _cursor += 2;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter AppendAscii(char c1, char c2, char c3)
    {
        Ensure(3);
        var ptr = Buffer + _cursor;
        *ptr++ = (byte)c1;
        *ptr++ = (byte)c2;
        *ptr = (byte)c3;
        _cursor += 3;
        return ref this;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append([InterpolatedStringHandlerArgument("")] ref NativeSpanWriterHandler handler)
    {
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter PadRight(int amount, byte value = 0x20)
    {
        Ensure(amount);
        var span = new Span<byte>(Buffer + _cursor, amount);
        span.Fill(value);
        _cursor += amount;
        return ref this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Ensure(int length)
    {
        // Null terminated last byte excluded from capacity 
        if ((uint)length + (uint)_cursor >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), length + _cursor, Capacity);
    }
}