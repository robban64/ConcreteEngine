using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Collections;

public sealed class SlotArray<T>
{
    private T[] _entries = [];
    private readonly Stack<int> _free = [];

    public int Count { get; private set; }

    public Action<SlotArray<T>>? OnResize;

    public int FreeCount => _free.Count;
    public int ActiveCount => Count - _free.Count;
    public int Capacity => _entries.Length;

    public SlotArray() { }

    public SlotArray(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _entries = new T[capacity];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int start = 0) => _entries.AsSpan(start, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int start, int length) => _entries.AsSpan(start, int.Min(length, Count));

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

    public void EnsureCapacity(int amount, int alignment = 64)
    {
        var len = Count + amount;
        if (_entries.Length >= len) return;

        var newSize = Arrays.CapacityGrowthSafe(_entries.Length, len);
        newSize = IntMath.AlignUp(newSize, alignment);

        Array.Resize(ref _entries, newSize);

        Console.WriteLine($"{GetType().Name}: resized {newSize}");
        OnResize?.Invoke(this);
    }

    public Span<T>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();
}