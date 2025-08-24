using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Common.Collections;
/*
public sealed class ArrayListBuffer<TKey, TData> where TKey : struct, IBinaryInteger<TKey>
{
    private readonly TData[] _buffer;
    private int[] _emptySlots;
    
    private int _idx = 1;
    private int _emptyIdx = 0;

    public ArrayListBuffer(int initialCapacity)
    {
        _buffer = new T[initialCapacity];
        _emptySlots = new int[initialCapacity];
    }

    public void Add(T item)
    {
        int idx = _emptyIdx > 0 ? _emptySlots[--_emptyIdx] : TryAllocate();
        _buffer[idx - 1] = item;
    }

    public void Remove(TKey key)
    {
        key.Value
        _array[key.Value?] // here is the issue
    }

    public T Get(int index) => _buffer[index];
}

public sealed class ArrayStructListBuffer<T> where T : struct
{
    private readonly T[] _buffer;
    private int[] _free;

    private int _idx = 0;

    public ArrayStructListBuffer(int initialCapacity)
    {
        _buffer = new T[initialCapacity];
        _free = new int[initialCapacity];
    }

    public void Add(T item)
    {
        _buffer[_idx++] = item;
    }

    public void Remove(T item)
    {
        _buffer[_idx--] = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(int index) => ref _buffer[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReadonly(int index) => ref _buffer[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetOut(int index, out T item) => item = _buffer[index];
}

*/
    
    