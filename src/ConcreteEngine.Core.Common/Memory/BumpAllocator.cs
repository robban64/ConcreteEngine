using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;

public sealed unsafe class BumpAllocator : IDisposable
{
    private NativeArray<byte> _buffer;

    private bool _hasBoundBuilder;

    public int Cursor { get; private set; }
    public int Capacity { get; }

    public MemoryBlock Tail { get; private set; }
    public MemoryBlock Head { get; private set; }

    public int Remaining => Capacity - Cursor;

    public BumpAllocator(int capacity, int alignment = 0, bool zeroed = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1024);
        if (IntMath.AlignUp(capacity, 64) != IntMath.AlignDown(capacity, 64))
            throw new ArgumentOutOfRangeException(nameof(capacity));

        if (alignment == 0)
            _buffer = NativeArray.Allocate<byte>(capacity, zeroed);
        else
            _buffer = NativeArray.AlignedAllocate<byte>(capacity, alignment, zeroed);

        Capacity = capacity;
    }


    public MemoryBlock AllocBlock(int length, bool zeroed = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, MemoryBlock.HeaderSize);
        if (_hasBoundBuilder) Throwers.InvalidOperation("Cannot allocate while having bound alloc builder");
        if (_buffer.IsNull) Throwers.NullPointer(nameof(_buffer));

        var blockSize = length + MemoryBlock.HeaderSize;

        if (Cursor + blockSize > Capacity)
            Throwers.BufferOverflow(nameof(BumpAllocator), Cursor + blockSize, Capacity);

        var memory = _buffer.Slice(Cursor, blockSize);
        Cursor += blockSize;

        if (zeroed) memory.Clear();
        var block = new MemoryBlock(new NativeView<byte>(memory.Ptr, length));

        if (Head == null) Head = block;
        else Tail.Ptr->Next = block.Ptr;

        return Tail = block;
    }

    public MemoryBlock AllocCommitBlock()
    {
        if (_buffer.IsNull) Throwers.NullPointer(nameof(_buffer));
        if (_hasBoundBuilder)
            Throwers.InvalidOperation("Cannot create new alloc builder while having bound alloc builder");

        if (Remaining <= 16) Throwers.BufferOverflow(nameof(BumpAllocator));
        _hasBoundBuilder = true;

        var block = new MemoryBlock(_buffer.Slice(Cursor));

        if (Head == null) Head = block;
        else Tail.Ptr->Next = block.Ptr;

        return Tail = block;
    }

    public MemoryBlock CommitBlock()
    {
        if (Tail.IsNull) Throwers.NullPointer(nameof(Tail));

        var basePtr = Tail.Data.Ptr - MemoryBlock.HeaderSize;
        ArgumentOutOfRangeException.ThrowIfNotEqual((nint)basePtr, (nint)Tail);
        ArgumentOutOfRangeException.ThrowIfNotEqual(Tail.Cursor, Tail.Cursor);

        int length = Tail.Cursor, totalLength = length + MemoryBlock.HeaderSize;
        if (Cursor + totalLength > Capacity)
            Throwers.BufferOverflow(nameof(BumpAllocator), Cursor + totalLength, Capacity);

        Cursor += totalLength;
        Tail.SetLength(length);
        _hasBoundBuilder = false;
        return Tail;
    }

    public void SetCursor(int cursor)
    {
        if ((uint)Cursor >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(BumpAllocator), Cursor, Capacity);

        Cursor = cursor;
    }

    public void Clear()
    {
        Cursor = 0;
        Head = null;
        Tail = null;
    }

    public void Dispose()
    {
        _buffer.Dispose();
        Head = null;
        Tail = null;
        Cursor = 0;
        _buffer.Ptr = null;
    }
}