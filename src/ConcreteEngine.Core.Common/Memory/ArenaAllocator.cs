using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Numerics;
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


    public ArenaBlockBuilder AllocBuilder()
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
        return new ArenaBlockBuilder(this, block);
    }

    private MemoryBlockPtr CommitBuilder(ArenaBlockBuilder builder)
    {
        if (builder.Memory.Ptr == null)
            throw new ArgumentException($"{nameof(builder.Memory)} cannot be null", nameof(builder));

        var builderPtr = builder.Memory.Ptr;
        ArgumentOutOfRangeException.ThrowIfNotEqual((nuint)builderPtr, (nuint)Tail);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(builderPtr->Cursor);

        int length = builderPtr->Cursor, totalLength = length + MemoryBlock.BlockSize;
        if (Cursor + totalLength > Capacity)
            throw new InsufficientMemoryException();

        Cursor += totalLength;
        builderPtr->SetLength(length);
        _hasBoundBuilder = false;
        return new MemoryBlockPtr(builderPtr);
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

    public readonly ref struct ArenaBlockBuilder
    {
        public readonly MemoryBlockPtr Memory;
        private readonly ArenaAllocator _allocator;

        internal ArenaBlockBuilder(ArenaAllocator allocator, MemoryBlock* memory)
        {
            Memory = new MemoryBlockPtr(memory);
            _allocator = allocator;
        }
        public NativeView<byte> AllocStringSlice(string str, int maxLength = 0)
        {
            var length = Encoding.UTF8.GetByteCount(str);
            if(maxLength > 0) length = int.Min(length, maxLength);

            var data = Memory.AllocSlice(length);
            data.Writer().Write(str.AsSpan().Slice(0, length));
            return data;
        }

        public NativeView<byte> AllocSlice(int length) => Memory.AllocSlice(length);
        public NativeView<T> AllocSlice<T>(int amount = 1) where T : unmanaged => Memory.AllocSlice<T>(amount);

        public MemoryBlockPtr Commit() => _allocator.CommitBuilder(this);
    }
}