using System.Text;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

internal static unsafe class NativeExtensions
{
    public static UnsafeSpanWriter Writer(this NativeViewPtr<byte> viewPtr) => new(viewPtr.Ptr, viewPtr.Length);
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

    public NativeViewPtr<byte> AllocWrap(int length)
    {
        if (_cursor + length > _capacity)
            _cursor = 0;

        var prevCursor = _cursor;
        byte* ptr = _buffer + _cursor;
        _cursor += length;
        return _buffer.Slice(prevCursor, length);
    }

    public NativeViewPtr<byte> Alloc(int length)
    {
        if (_cursor + length > _capacity)
            throw new InsufficientMemoryException();

        var prevCursor = _cursor;
        byte* ptr = _buffer + _cursor;
        _cursor += length;
        return _buffer.Slice(prevCursor, length);
    }

    public NativeViewPtr<byte> AllocString(string str)
    {
        var length = Encoding.UTF8.GetByteCount(str);
        var ptr = Alloc(length);
        ptr.Writer().Write(str);
        return ptr;
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