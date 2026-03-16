using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

internal static unsafe class NativeExtensions
{
    public static UnsafeSpanWriter Writer(this NativeViewPtr<byte> viewPtr) => new(viewPtr.Ptr, viewPtr.Length);
}

internal unsafe struct ArenaBlock(NativeViewPtr<byte> current)
{
    public NativeViewPtr<byte> Current = current;

    private int _cursor;
    
    public bool IsNull => Current.IsNull;

    public NativeViewPtr<byte> AllocSlice(int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, Current.Length);
        var start =  _cursor;
        _cursor += length;
        return Current.Slice(start, length);
    }
}

internal sealed unsafe class ArenaAllocator : IDisposable
{
    private NativeArray<byte> _buffer;
    private readonly int _capacity;
    private int _cursor;

    private ArenaBlock _tail;
    private ArenaBlock _head;

    public ArenaAllocator(int capacity = 1024)
    {
        _buffer = NativeArray.Allocate<byte>(capacity);
        _capacity = capacity;
    }

    public ArenaBlock Alloc(int length, bool zeroing = false)
    {
        if (_cursor + length > _capacity)
            throw new InsufficientMemoryException();

        var view = _buffer.Slice(_cursor, length);
        _cursor += length;

        if (zeroing) view.Clear();

        var block = new ArenaBlock(view);
        if(_tail.IsNull) _tail = block;
       return _head = block;
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