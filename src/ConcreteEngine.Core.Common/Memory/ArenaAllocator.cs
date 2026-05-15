using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;

public sealed unsafe class ArenaAllocator : IDisposable
{
    private bool _hasBoundBuilder;

    public int Cursor { get; private set; }
    public int Capacity { get; }

    private NativeArray<byte> _buffer;

    public MemoryBlock* Tail;
    public MemoryBlock* Head;

    public ArenaAllocator(int capacity = 1024, bool zeroed = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1024);
        if (IntMath.AlignUp(capacity, 64) != IntMath.AlignDown(capacity, 64))
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _buffer = NativeArray.Allocate<byte>(capacity, zeroed);
        Capacity = capacity;
    }


    public int Remaining => Capacity - Cursor;

    public MemoryBlockPtr GetTail() => Tail;
    public MemoryBlockPtr GetHead() => Head;


    public MemoryBlockPtr AllocBlock(int blockSize, bool zeroed = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(blockSize, MemoryBlock.BlockSize);

        if (_hasBoundBuilder)
            throw new InvalidOperationException("Cannot allocate while having bound alloc builder");

        if (_buffer.IsNull)
            throw new InvalidOperationException("Buffer is null");

        if (Cursor + blockSize > Capacity)
            throw new InsufficientMemoryException();

        var memory = _buffer.Slice(Cursor, blockSize);
        Cursor += blockSize;

        if (zeroed) memory.Clear();

        var block = (MemoryBlock*)memory.Ptr;
        block->Init(blockSize - MemoryBlock.BlockSize);

        if (Head == null)
            Head = block;
        else
            Tail->Next = block;

        return Tail = block;
    }

    public MemoryBlockPtr Alloc(int size, bool zeroed = false)
    {
        return AllocBlock(size + MemoryBlock.BlockSize, zeroed);
    }


    public NativeAllocator MakeBuilder()
    {
        if (_hasBoundBuilder)
            throw new InvalidOperationException("Cannot create new alloc builder while having bound alloc builder");

        if (Remaining <= 16) throw new InsufficientMemoryException();
        _hasBoundBuilder = true;

        var memory = _buffer.Slice(Cursor);
        var block = (MemoryBlock*)memory.Ptr;
        block->Init(memory.Length);

        if (Head == null)
            Head = block;
        else
            Tail->Next = block;

        Tail = block;

        return block->GetAllocator();
    }

    public MemoryBlockPtr CommitBuilder(NativeAllocator builder)
    {
        if (builder.IsNull)
            throw new ArgumentException($"{nameof(builder.Data)} cannot be null", nameof(builder));

        var basePtr = builder.Data.Ptr - MemoryBlock.BlockSize;
        ArgumentOutOfRangeException.ThrowIfNotEqual((nuint)basePtr, (nuint)Tail);
        ArgumentOutOfRangeException.ThrowIfNotEqual(builder.Cursor, Tail->Cursor);

        int length = Tail->Cursor, totalLength = length + MemoryBlock.BlockSize;
        if (Cursor + totalLength > Capacity)
            throw new InsufficientMemoryException();

        Cursor += totalLength;
        Tail->SetLength(length);
        _hasBoundBuilder = false;
        return new MemoryBlockPtr(Tail);
    }


    public void SetCursor(int cursor)
    {
        if ((uint)Cursor > (uint)Capacity)
            throw new ArgumentOutOfRangeException(nameof(cursor));

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