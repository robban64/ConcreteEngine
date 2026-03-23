using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

internal static unsafe class NativeExtensions
{
    public static UnsafeSpanWriter Writer(this NativeViewPtr<byte> viewPtr) => new(viewPtr.Ptr, viewPtr.Length);
}

internal unsafe struct ArenaBlock
{
    public ArenaBlock* Next;

    // public T* Ptr = ptr;
    // public readonly int Offset = offset;
    // public readonly int Length = length;
    public NativeViewPtr<byte> Data;
    private int _cursor;

    public bool HasNullPtr => Data.IsNull;
    public int Remaining => Data.Length - _cursor;

    public void Init(NativeViewPtr<byte> data)
    {
        Next = null;
        Data = data;
        _cursor = 0;
    }

    public NativeViewPtr<byte> AllocSlice(int length)
    {
        if (_cursor + length > Data.Length)
            throw new InsufficientMemoryException(length.ToString());

        var start = _cursor;
        _cursor += length;
        return Data.Slice(start, length);
    }
}

internal sealed unsafe class ArenaAllocator : IDisposable
{
    private static int BlockSize => Unsafe.SizeOf<ArenaBlock>();

    private NativeArray<byte> _buffer;
    private readonly int _capacity;
    private int _cursor;

    private ArenaBlock* _tail;
    private ArenaBlock* _head;


    public ArenaAllocator(int capacity = 1024)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1024);
        if (!IntMath.IsPowerOfTwo(capacity))
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _buffer = NativeArray.Allocate<byte>(capacity);
        _capacity = capacity;
    }

    public int Remaining => _capacity - _cursor;

    public ArenaBlock* Alloc(int length, bool zeroed = false)
    {
        var totalLength = length + BlockSize;
        if (_cursor + totalLength > _capacity)
            throw new InsufficientMemoryException();

        var viewPtr = _buffer.Slice(_cursor, totalLength);
        _cursor += totalLength;

        if (zeroed) viewPtr.Clear();

        var block = (ArenaBlock*)viewPtr.Ptr;
        block->Init(
            viewPtr.SliceFrom(BlockSize)); // block.cursor = 0 and offset ptr otherwise would include the ArenaBlock

        if (_head == null)
            _head = block;
        else
            _tail->Next = block;

        _tail = block;
        return block;
    }

    public void SetCursor(int cursor)
    {
        if ((uint)_cursor > (uint)_capacity)
            throw new ArgumentOutOfRangeException(nameof(cursor));

        _cursor = cursor;
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