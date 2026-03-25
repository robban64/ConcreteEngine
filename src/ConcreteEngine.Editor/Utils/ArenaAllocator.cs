using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

internal static unsafe class NativeExtensions
{
    public static UnsafeSpanWriter Writer(this NativeViewPtr<byte> viewPtr) => new(viewPtr.Ptr, viewPtr.Length);
}

//TODO shink to 16 bytes?
internal unsafe struct ArenaBlock
{
    public static int BlockSize => Unsafe.SizeOf<ArenaBlock>();

    public ArenaBlock* Next;
    private int _length;
    private int _cursor;

    public NativeViewPtr<byte> DataPtr => !Unsafe.IsNullRef(ref this)
        ? new NativeViewPtr<byte>((byte*)Unsafe.AsPointer(ref this) + BlockSize, 0, _length)
        : throw new NullReferenceException("ArenaBlock pointer is null");

    public int Remaining => _length - _cursor;

    public void Init(NativeViewPtr<byte> data)
    {
        Next = null;
        _length = data.Length;
        _cursor = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public NativeViewPtr<byte> AllocSlice(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(IntMath.AlignUp(length, 4), length, nameof(length));

        length = IntMath.AlignUp(length, 4);
        if ((uint)_cursor + (uint)length > (uint)_length)
            throw new InsufficientMemoryException(length.ToString());

        var start = _cursor;
        _cursor += length;
        return DataPtr.Slice(start, length);
    }

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

internal sealed unsafe class ArenaAllocator : IDisposable
{
    private NativeArray<byte> _buffer;
    private readonly int _capacity;
    private int _cursor;

    public ArenaBlock* Tail;
    public ArenaBlock* Head;


    public ArenaAllocator(int capacity = 1024)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1024);
        if (IntMath.AlignUp(capacity, 64) != IntMath.AlignDown(capacity, 64))
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _buffer = NativeArray.Allocate<byte>(capacity);
        _capacity = capacity;
    }

    public int Remaining => _capacity - _cursor;

    public ArenaBlock* Alloc(int length, bool zeroed = false)
    {
        var totalLength = length + ArenaBlock.BlockSize;
        if (_cursor + totalLength > _capacity)
            throw new InsufficientMemoryException();

        var memory = _buffer.Slice(_cursor, totalLength);
        _cursor += totalLength;

        if (zeroed) memory.Clear();

        var block = (ArenaBlock*)memory.Ptr;
        block->Init(memory.SliceFrom(ArenaBlock.BlockSize));

        if (Head == null)
            Head = block;
        else
            Tail->Next = block;

        Tail = block;
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