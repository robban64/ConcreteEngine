using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Core.Common.Memory;

public readonly unsafe struct ArenaBlockPtr(ArenaBlock* ptr)
{
    public static int BlockSize => ArenaBlock.BlockSize;
    public bool IsNull => Ptr == null;

    public readonly ArenaBlock* Ptr = ptr;

    public ArenaBlockPtr Next => new(Ptr->Next);
    public NativeViewPtr<byte> DataPtr => Ptr->DataPtr;

    public int Cursor => Ptr->Cursor;
    public int Length => Ptr->Length;
    public int Remaining => Ptr->Remaining;
    
    internal void SetLength(int length) => Ptr->SetLength(length);

    public static implicit operator ArenaBlockPtr(ArenaBlock* ptr) => new(ptr);
    public static implicit operator ArenaBlockPtr(IntPtr ptr) => new((ArenaBlock*)ptr);
    public static explicit operator IntPtr(ArenaBlockPtr ptr) => (IntPtr)ptr.Ptr;


    public NativeViewPtr<byte> AllocSlice(int length) => Ptr->AllocSlice(length);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public NativeViewPtr<byte> AllocStringSlice(ReadOnlySpan<char> str)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(str.Length);
        var len = Encoding.UTF8.GetByteCount(str) + 1;
        var slice = AllocSlice(len);
        slice.Writer().Write(str);
        return slice;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public NativeViewPtr<T> AllocSlice<T>(int amount = 1) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        return AllocSlice(Unsafe.SizeOf<T>() * amount).Reinterpret<T>();
    }
}

public unsafe struct ArenaBlock
{
    public static readonly int BlockSize = Unsafe.SizeOf<ArenaBlock>();

    public ArenaBlock* Next;
    private int _length;
    private int _cursor;

    public NativeViewPtr<byte> DataPtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get =>
            !Unsafe.IsNullRef(ref this)
                ? new NativeViewPtr<byte>((byte*)Unsafe.AsPointer(ref this) + BlockSize, 0, _length)
                : throw new NullReferenceException("ArenaBlock pointer is null");
    }

    public readonly int Cursor => _cursor;
    public readonly int Length => _length;
    public readonly int Remaining => _length - _cursor;

    internal void Init(int length)
    {
        Next = null;
        _length = length;
        _cursor = 0;
    }

    internal void SetLength(int length) => _length = length;

    public NativeViewPtr<byte> AllocSlice(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 4);

        length = IntMath.AlignUp(length, 4);
        if ((uint)_cursor + (uint)length > (uint)_length)
            throw new InsufficientMemoryException(length.ToString());

        var start = _cursor;
        _cursor += length;
        return DataPtr.Slice(start, length);
    }
}

public sealed unsafe class ArenaAllocator : IDisposable
{
    private bool _hasBoundBuilder;
    public int Cursor { get; private set; }
    public int Capacity { get; }

    private NativeArray<byte> _buffer;

    public ArenaBlock* Tail;
    public ArenaBlock* Head;

    public ArenaAllocator(int capacity = 1024, bool zeroed = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1024);
        if (IntMath.AlignUp(capacity, 64) != IntMath.AlignDown(capacity, 64))
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _buffer = NativeArray.Allocate<byte>(capacity, zeroed);
        Capacity = capacity;
    }

    public int Remaining => Capacity - Cursor;


    public ArenaBlockPtr AllocBlock(int blockSize, bool zeroed = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(blockSize, ArenaBlock.BlockSize);

        if (_hasBoundBuilder)
            throw new InvalidOperationException("Cannot allocate while having bound alloc builder");

        if (_buffer.IsNull)
            throw new InvalidOperationException("Buffer is null");

        if (Cursor + blockSize > Capacity)
            throw new InsufficientMemoryException();

        var memory = _buffer.Slice(Cursor, blockSize);
        Cursor += blockSize;

        if (zeroed) memory.Clear();

        var block = (ArenaBlock*)memory.Ptr;
        block->Init(blockSize - ArenaBlock.BlockSize);

        if (Head == null)
            Head = block;
        else
            Tail->Next = block;

        return Tail = block;
    }

    public ArenaBlockPtr Alloc(int size, bool zeroed = false)
    {
        return AllocBlock(size + ArenaBlock.BlockSize, zeroed);
    }


    public ArenaBlockBuilder AllocBuilder()
    {
        if (_hasBoundBuilder)
            throw new InvalidOperationException("Cannot create new alloc builder while having bound alloc builder");

        if (Remaining <= 16) throw new InsufficientMemoryException();
        _hasBoundBuilder = true;

        var memory = _buffer.Slice(Cursor);
        var block = (ArenaBlock*)memory.Ptr;
        block->Init(memory.Length);

        if (Head == null)
            Head = block;
        else
            Tail->Next = block;

        Tail = block;
        return new ArenaBlockBuilder(this, block);
    }

    private ArenaBlockPtr CommitBuilder(ArenaBlockBuilder builder)
    {
        if (builder.Memory.Ptr == null)
            throw new ArgumentException($"{nameof(builder.Memory)} cannot be null", nameof(builder));

        var builderPtr = builder.Memory.Ptr;
        ArgumentOutOfRangeException.ThrowIfNotEqual((nuint)builderPtr, (nuint)Tail);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(builderPtr->Cursor);

        int length = builderPtr->Cursor, totalLength = length + ArenaBlock.BlockSize;
        if (Cursor + totalLength > Capacity)
            throw new InsufficientMemoryException();

        Cursor += totalLength;
        builderPtr->SetLength(length);
        _hasBoundBuilder = false;
        return new ArenaBlockPtr(builderPtr);
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
        public readonly ArenaBlockPtr Memory;
        private readonly ArenaAllocator _allocator;

        internal ArenaBlockBuilder(ArenaAllocator allocator, ArenaBlock* memory)
        {
            Memory = new ArenaBlockPtr(memory);
            _allocator = allocator;
        }

        public NativeViewPtr<byte> AllocSlice(int length) => Memory.AllocSlice(length);
        public NativeViewPtr<byte> AllocStringSlice(ReadOnlySpan<char> str) => Memory.AllocStringSlice(str);
        public NativeViewPtr<T> AllocSlice<T>(int amount = 1) where T : unmanaged => Memory.AllocSlice<T>(amount);

        public ArenaBlockPtr Commit() => _allocator.CommitBuilder(this);
    }
}