using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeSpanWriter(NativeView<byte> buffer) : this(buffer, buffer.Length) { }

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
        return new NativeView<byte>(Buffer, 0, cursor);
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