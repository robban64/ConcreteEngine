using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Common.Text;

public unsafe struct UnsafeSpanWriter(byte* buffer, int capacity)
{
    public UnsafeSpanWriter(NativeView<byte> buffer) : this(buffer, buffer.Length) { }

    public readonly byte* Buffer = buffer;
    public readonly int Capacity = capacity;
    private int _cursor;

    public readonly int Cursor => _cursor;
    public readonly int BytesLeft => Capacity - _cursor;

    public void Clear() => _cursor = 0;
    public void SetCursor(int cursor) => _cursor = cursor;

    public readonly Span<byte> AsSpan(int start = 0) => new(Buffer + start, Capacity - start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly UnsafeSpanWriter Slice(int start = 0) => new(Buffer + _cursor + start, Capacity - _cursor - start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<byte> Next()
    {
        var cursor = _cursor;
        Buffer[cursor] = 0;
        _cursor++;
        return new NativeView<byte>(Buffer, 0, cursor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<byte> End()
    {
        Buffer[_cursor] = 0;
        var view = new NativeView<byte>(Buffer, 0, _cursor);
        _cursor = 0;
        return view;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> EndSpan() => End().AsSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write(char value)
    {
        var written = UtfText.FormatChar(ref *Buffer, value);
        Buffer[written] = 0;
        return new NativeView<byte>(Buffer, written);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write(int value)
    {
        var written = UtfText.Format(value, ref *Buffer, Capacity);
        Buffer[written] = 0;
        return new NativeView<byte>(Buffer, written);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write(uint value)
    {
        var written = UtfText.Format(value, ref *Buffer, Capacity);
        Buffer[written] = 0;
        return new NativeView<byte>(Buffer, written);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        {
            Buffer[0] = 0;
            return new NativeView<byte>(Buffer, 0);
        }

        Unsafe.CopyBlockUnaligned(ref *Buffer, ref MemoryMarshal.GetReference(value), (uint)value.Length);
        Buffer[value.Length] = 0;
        return new NativeView<byte>(Buffer, value.Length);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            Buffer[0] = 0;
            return new NativeView<byte>(Buffer, 0);
        }

        var dest = MemoryMarshal.CreateSpan(ref *Buffer, Capacity - 1);
        Utf8.FromUtf16(value, dest, out _, out var written);
        Buffer[written] = 0;
        return new NativeView<byte>(Buffer, written);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write<T>(T value, ReadOnlySpan<char> format = default) where T : IUtf8SpanFormattable
    {
        var dest = MemoryMarshal.CreateSpan(ref *Buffer, Capacity - 1);
        value.TryFormat(dest, out var written, format, null);
        Buffer[written] = 0;
        return new NativeView<byte>(Buffer, written);
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(byte* value)
    {
        if (value == null) return ref this;
        var index = UtfText.GetNullTerminateIndex(ref *value);
        Unsafe.CopyBlockUnaligned(ref Buffer[_cursor], ref *value, (uint)index);
        _cursor += index;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty) return ref this;
        Unsafe.CopyBlockUnaligned(ref Buffer[_cursor], ref MemoryMarshal.GetReference(value), (uint)value.Length);
        _cursor += value.Length;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty) return ref this;
        var dest = MemoryMarshal.CreateSpan(ref *(Buffer + _cursor), Capacity - _cursor);
        Utf8.FromUtf16(value, dest, out _, out var written);
        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(char value)
    {
        var written = UtfText.FormatChar(ref *(Buffer + _cursor), value);
        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(int value)
    {
        var written = UtfText.Format(value, ref *(Buffer + _cursor), Capacity - _cursor);
        _cursor += written;
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter Append(uint value)
    {
        var written = UtfText.Format(value, ref *(Buffer + _cursor), Capacity - _cursor);
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

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref UnsafeSpanWriter PadRight(int amount, byte value = 0x20)
    {
        int safeAmount = Math.Min(amount, Capacity - _cursor);
        NativeMemory.Fill(Buffer + _cursor, (nuint)safeAmount, value);
        _cursor += amount;
        return ref this;
    }
}