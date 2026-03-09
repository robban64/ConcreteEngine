using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

internal unsafe struct ArenaPtr(byte* value, int cursor, int length)
{
    public byte* Ptr = value;

    public readonly int Length = length;
    public readonly int Cursor = cursor;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte*(ArenaPtr str) => str.Ptr;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte* operator +(ArenaPtr a, int b) => a.Ptr + b;

    public void Clear()
    {
        NativeMemory.Clear(Ptr, (nuint)Length);
    }

    public void CopyFrom(ReadOnlySpan<byte> span)
    {
        if(span.Length > Length) throw new InsufficientMemoryException();
        var dst = new Span<byte>(Ptr, Length);
        span.CopyTo(dst);
    }
    
    public UnsafeSpanWriter Writer() => new (Ptr,Length);
}

internal sealed unsafe class ArenaAllocator : IDisposable
{
    private NativeArray<byte> _buffer;
    private int _capacity;
    private int _cursor;

    public ArenaAllocator(int capacity = 1024)
    {
        _buffer = NativeArray.Allocate<byte>(capacity);
        _capacity = capacity;
    }

    public ArenaPtr Alloc(int length)
    {
        if(_cursor + length > _capacity) 
            throw new InsufficientMemoryException();

        var prevCursor = _cursor;
        byte* ptr = _buffer + _cursor;
        _cursor += length;
        return new ArenaPtr(ptr,prevCursor,length);
    }

    public void Reset()
    {
        _cursor = 0;
    }

    public void Dispose()
    {
        _buffer.Dispose();
    }
}