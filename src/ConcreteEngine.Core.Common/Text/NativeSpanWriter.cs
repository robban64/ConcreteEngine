using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Common.Text;

public unsafe ref struct NativeSpanWriter(byte* buffer, int capacity)
{
    public NativeSpanWriter(NativeView<byte> buffer) : this(buffer, buffer.Length) { }

    public readonly byte* Buffer = buffer;
    public readonly int Capacity = capacity;
    private int _cursor;

    public readonly int Cursor => _cursor;
    public readonly int BytesLeft => Capacity - _cursor;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _cursor = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCursor(int cursor)
    {
        if ((uint)cursor >= (uint)Capacity) 
            Throwers.BufferOverflow(nameof(NativeSpanWriter), cursor, Capacity);
        _cursor = cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> AsSpan(int start = 0) =>
        MemoryMarshal.CreateSpan(ref *(Buffer + start), Capacity - start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> EndSpan() => End().AsSpan();

    public NativeView<byte> End()
    {
        var cursor = _cursor;
        _cursor = 0;
        Buffer[cursor] = 0;
        return new NativeView<byte>(Buffer, 0, cursor);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write(char value)
    {
        var written = UtfText.FormatChar(ref *Buffer, value);
        if ((uint)Cursor + (uint)written >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), Cursor + written, Capacity);

        Buffer[written] = 0;
        return new NativeView<byte>(Buffer, written);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write(int value)
    {
        var written = UtfText.Format(value, ref *Buffer, Capacity);
        if ((uint)Cursor + (uint)written >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), Cursor + written, Capacity);

        Buffer[written] = 0;
        return new NativeView<byte>(Buffer, written);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write(uint value)
    {
        var written = UtfText.Format(value, ref *Buffer, Capacity);
        if ((uint)Cursor + (uint)written >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), Cursor + written, Capacity);

        Buffer[written] = 0;
        return new NativeView<byte>(Buffer, written);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write(scoped ReadOnlySpan<byte> value)
    {
        if (value.Length == 0)
        {
            Buffer[0] = 0;
            return new NativeView<byte>(Buffer, 0);
        }

        if ((uint)Cursor + (uint)value.Length >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), Cursor + value.Length, Capacity);

        Unsafe.CopyBlockUnaligned(ref *Buffer, ref MemoryMarshal.GetReference(value), (uint)value.Length);
        Buffer[value.Length] = 0;
        return new NativeView<byte>(Buffer, value.Length);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write(scoped ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
        {
            Buffer[0] = 0;
            return new NativeView<byte>(Buffer, 0);
        }

        if ((uint)Cursor + (uint)value.Length >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), Cursor + value.Length, Capacity);

        var dest = MemoryMarshal.CreateSpan(ref *Buffer, Capacity - 1);
        var written = Encoding.UTF8.GetBytes(value, dest);
        Buffer[written] = 0;
        return new NativeView<byte>(Buffer, written);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write<T>(T value, ReadOnlySpan<char> format = default)
        where T : IUtf8SpanFormattable
    {
        var dest = MemoryMarshal.CreateSpan(ref *Buffer, Capacity - 1);
        value.TryFormat(dest, out var written, format, null);
        if ((uint)Cursor + (uint)written >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), Cursor + written, Capacity);

        Buffer[written] = 0;
        return new NativeView<byte>(Buffer, written);
    }


    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(byte* value)
    {
        if (value == null) return ref this;
        var src = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(value);
        if ((uint)Cursor + (uint)src.Length >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), Cursor + src.Length, Capacity);

        Unsafe.CopyBlockUnaligned(ref Buffer[_cursor], ref MemoryMarshal.GetReference(src), (uint)src.Length);
        _cursor += src.Length;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(NativeView<byte> value)
    {
        if (value.Length == 0) return ref this;
        if ((uint)Cursor + (uint)value.Length >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), Cursor + value.Length, Capacity);

        Unsafe.CopyBlockUnaligned(Buffer + _cursor, value, (uint)value.Length);
        _cursor += value.Length;
        return ref this;
    }


    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(scoped ReadOnlySpan<byte> value)
    {
        if (value.Length == 0) return ref this;
        if ((uint)Cursor + (uint)value.Length >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), Cursor + value.Length, Capacity);

        Unsafe.CopyBlockUnaligned(ref Buffer[_cursor], ref MemoryMarshal.GetReference(value), (uint)value.Length);
        _cursor += value.Length;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(scoped ReadOnlySpan<char> value)
    {
        if (value.Length == 0) return ref this;
        if ((uint)Cursor + (uint)value.Length >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), Cursor + value.Length, Capacity);

        var dest = MemoryMarshal.CreateSpan(ref *(Buffer + _cursor), Capacity - _cursor);
        var written = Encoding.UTF8.GetBytes(value, dest);
        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(char value)
    {
        var written = UtfText.FormatChar(ref *(Buffer + _cursor), value);
        if ((uint)Cursor + (uint)written >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), Cursor + written, Capacity);

        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(int value)
    {
        var written = UtfText.Format(value, ref *(Buffer + _cursor), Capacity - _cursor);
        if ((uint)Cursor + (uint)written >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), Cursor + written, Capacity);

        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append(uint value)
    {
        var written = UtfText.Format(value, ref *(Buffer + _cursor), Capacity - _cursor);
        if ((uint)Cursor + (uint)written >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), Cursor + written, Capacity);

        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter Append<T>(T value, ReadOnlySpan<char> format = default) where T : IUtf8SpanFormattable
    {
        value.TryFormat(AsSpan(_cursor), out var written, format, null);
        if ((uint)Cursor + (uint)written >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), Cursor + written, Capacity);

        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref NativeSpanWriter PadRight(int amount, byte value = 0x20)
    {
        amount = int.Clamp(amount,0, Capacity - _cursor);
        NativeMemory.Fill(Buffer + _cursor, (nuint)amount, value);
        _cursor += amount;
        return ref this;
    }
    
}