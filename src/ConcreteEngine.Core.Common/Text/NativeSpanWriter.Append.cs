using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Common.Text;

[InterpolatedStringHandler]
public unsafe ref partial struct NativeSpanWriter
{
    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(byte* value)
    {
        if (value == null) return ref this;
        var src = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(value);
        if (Validate(src.Length))
            Unsafe.CopyBlockUnaligned(ref Buffer[_cursor], ref MemoryMarshal.GetReference(src), (uint)src.Length);

        _cursor += src.Length;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(NativeView<byte> value)
    {
        if (Validate(value.Length)) Unsafe.CopyBlockUnaligned(Buffer + _cursor, value, (uint)value.Length);
        _cursor += value.Length;
        return ref this;
    }


    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(scoped ReadOnlySpan<byte> value)
    {
        if (Validate(value.Length))
            Unsafe.CopyBlockUnaligned(ref Buffer[_cursor], ref MemoryMarshal.GetReference(value), (uint)value.Length);

        _cursor += value.Length;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(scoped ReadOnlySpan<char> value)
    {
        if (Validate(value.Length)) _cursor += Encoding.UTF8.GetBytes(value, RemainingSpan());
        return ref this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append([InterpolatedStringHandlerArgument("")] ref NativeSpanWriterHandler handler)
    {
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(byte value)
    {
        Debug.Assert(_cursor + 1 < Capacity);
        Buffer[_cursor] = value;
        _cursor += 1;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(char value)
    {
        Debug.Assert(_cursor + 2 < Capacity);
        _cursor += UtfText.FormatChar(ref Buffer[_cursor], value);
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(int value)
    {
        _cursor += UtfText.Format(value, ref Buffer[_cursor], BytesLeft);
        Debug.Assert(_cursor < Capacity);
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(uint value)
    {
        _cursor += UtfText.Format(value, ref Buffer[_cursor], BytesLeft);
        Debug.Assert(_cursor < Capacity);
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
        var cursor = _cursor;
        Debug.Assert(_cursor + 1 < Capacity);
        Buffer[cursor] = (byte)c1;
        _cursor += 1;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter AppendAscii(char c1, char c2)
    {
        var cursor = _cursor;
        Debug.Assert(_cursor + 2 < Capacity);
        Buffer[cursor + 0] = (byte)c1;
        Buffer[cursor + 1] = (byte)c2;
        _cursor += 2;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter AppendAscii(char c1, char c2, char c3)
    {
        var cursor = _cursor;
        Debug.Assert(_cursor + 3 < Capacity);
        Buffer[cursor + 0] = (byte)c1;
        Buffer[cursor + 1] = (byte)c2;
        Buffer[cursor + 2] = (byte)c3;
        _cursor += 3;
        return ref this;
    }
}