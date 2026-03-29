using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Collections;

//TODO
public sealed class SlotArray<T>
{
    private T[] _entries = [];
    private readonly Stack<int> _free = [];

    public int Count { get; private set; }

    public int FreeCount => _free.Count;
    public int ActiveCount => Count - _free.Count;
    public int Capacity => _entries.Length;

    public SlotArray() { }

    public SlotArray(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _entries = new T[capacity];
    }

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _entries[index];
    }

    public void Add(T entry)
    {
        var index = AllocateNext();
        _entries[index] = entry;
    }


    public void Remove(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _entries.Length);
        _entries[index] = default!;
        _free.Push(index);
    }

    public int AllocateNext()
    {
        if (_free.TryPop(out var index))
            return index;

        EnsureCapacity(1);
        return Count++;
    }

    public void EnsureCapacity(int amount)
    {
        var len = Count + amount;
        if (_entries.Length >= len) return;

        var newSize = Arrays.CapacityGrowthSafe(_entries.Length, len);
        newSize = IntMath.AlignUp(newSize, 32);

        Array.Resize(ref _entries, newSize);

        Console.WriteLine($"{GetType().Name}: resized {newSize}");

        //foreach (var callback in _onResizeCallbacks)
        //    callback(this);
    }

    public Span<T>.Enumerator GetEnumerator() => _entries.AsSpan().GetEnumerator();
}