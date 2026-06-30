using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;

public sealed unsafe class ArenaAllocator : IDisposable
{
    private bool _hasBoundBuilder;

    public int Cursor { get; private set; }
    public int Capacity { get; }

    private NativeArray<byte> _buffer;

    public MemoryBlockPtr Tail { get; private set; }
    public MemoryBlockPtr Head  { get; private set; }

    public ArenaAllocator(int capacity, int alignment = 0, bool zeroed = true)
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


    public int Remaining => Capacity - Cursor;

    public MemoryBlockPtr AllocBlock(int length, bool zeroed = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, MemoryBlockPtr.BlockSize);

        if (_hasBoundBuilder)
            Throwers.InvalidOperation("Cannot allocate while having bound alloc builder");

        if (_buffer.IsNull) Throwers.NullPointer(nameof(_buffer));

        var blockSize = length + MemoryBlockPtr.BlockSize;

        if (Cursor + blockSize > Capacity)
            Throwers.BufferOverflow(nameof(ArenaAllocator), Cursor + blockSize, Capacity);

        var memory = _buffer.Slice(Cursor, blockSize);
        Cursor += blockSize;

        if (zeroed) memory.Clear();
        var block = new MemoryBlockPtr(new NativeView<byte>(memory.Ptr, length));
        
        if (Head == null)
            Head = block;
        else
            Tail.Ptr->Next = block.Ptr;

        return Tail = block;
    }

    public NativeAllocator MakeBuilder()
    {
        if (_hasBoundBuilder)
            Throwers.InvalidOperation("Cannot create new alloc builder while having bound alloc builder");

        if (Remaining <= 16) Throwers.BufferOverflow(nameof(ArenaAllocator));
        _hasBoundBuilder = true;

        var block = new MemoryBlockPtr(_buffer.Slice(Cursor));

        if (Head == null)
            Head = block;
        else
            Tail.Ptr->Next = block.Ptr;

        Tail = block;

        return block.GetAllocator();
    }

    public MemoryBlockPtr CommitBuilder(NativeAllocator builder)
    {
        if (builder.IsNull)
            Throwers.InvalidArgument("builder.Data cannot be null", nameof(builder));

        var basePtr = builder.Data.Ptr - MemoryBlockPtr.BlockSize;
        ArgumentOutOfRangeException.ThrowIfNotEqual((nint)basePtr, (nint)Tail);
        ArgumentOutOfRangeException.ThrowIfNotEqual(builder.Cursor, Tail.Cursor);

        int length = Tail.Cursor, totalLength = length + MemoryBlockPtr.BlockSize;
        if (Cursor + totalLength > Capacity)
            Throwers.BufferOverflow(nameof(ArenaAllocator),Cursor + totalLength, Capacity);

        Cursor += totalLength;
        Tail.SetLength(length);
        _hasBoundBuilder = false;
        return Tail;
    }


    public bool CanAlloc(int capacity) => capacity + MemoryBlockPtr.BlockSize < Head.Remaining;

    public void SetCursor(int cursor)
    {
        if ((uint)Cursor >= (uint)Capacity)
            Throwers.BufferOverflow(nameof(ArenaAllocator),Cursor, Capacity);

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