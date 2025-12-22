using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Generics;

namespace ConcreteEngine.Common.Memory;

public unsafe struct NativeList<T> : IDisposable where T : unmanaged
{
    private T* _ptr;
    private int _capacity;
    private int _count;

    public NativeList(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 4);

        _capacity = capacity;
        _count = 0;
        _ptr = (T*)NativeMemory.AlignedAlloc((nuint)(capacity * Unsafe.SizeOf<T>()), 16);
    }

    public ValuePtr<T> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref _ptr[index]);
    }

    public Span<T> AsSpan() => new(_ptr, _count);

    public void Add(in T item)
    {
        if (_count >= _capacity) Resize();
        _ptr[_count++] = item;
    }

    private void Resize()
    {
        var newCap = _capacity * 2;
        var newPtr = (T*)NativeMemory.AlignedRealloc(_ptr, (nuint)(newCap * sizeof(T)), 16);
        _ptr = newPtr;
        _capacity = newCap;
    }


    public void Dispose()
    {
        if (_ptr == null)return;

        NativeMemory.AlignedFree(_ptr);
        _ptr = null;
    }
}