using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Collections;

public sealed class SlotArray<T> where T : class
{
    private T?[] _entries;
    private readonly Stack<int> _free = [];

    public int Count { get; private set; }

    public Action<int>? OnResize;

    public int FreeCount => _free.Count;
    public int ActiveCount => Count - _free.Count;
    public int Capacity => _entries.Length;

    public SlotArray(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _entries = new T[capacity];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T?> AsSpan(int start = 0) => _entries.AsSpan(start, Count);

    public ref T? this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_entries.Length, nameof(index));
            return ref _entries[index];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(int index) => (uint)index < (uint)_entries.Length && _entries[index] != null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? GetOrNull(int index) => (uint)index < (uint)_entries.Length ? _entries[index] : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(int index, [NotNullWhen(true)] out T? entry)
    {
        entry = GetOrNull(index);
        return entry != null;
    }

    public int Add(T entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var index = AllocateNext();
        _entries[index] = entry;
        return index;
    }

    public int AllocateNext()
    {
        var index = SlotHelper.NextSlot(_free, Count);
        if (index >= 0) return index;

        if (Count >= Capacity) EnsureCapacity(1);
        return Count++;
    }

    public void Remove(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)Count, nameof(index));
        Count = SlotHelper.FreeSlotAndClearStale(_free, index, Count, _entries);
    }

    public void Clear()
    {
        Array.Clear(_entries, 0, Count);
        _free.Clear();
        Count = 0;
    }

    public void EnsureCapacity(int amount, int alignment = 64)
    {
        var len = Count + amount;
        if (_entries.Length >= len) return;

        var newSize = CapacityUtils.CapacityGrowthSafe(_entries.Length, len);
        newSize = IntMath.AlignUp(newSize, alignment);

        Array.Resize(ref _entries, newSize);

        OnResize?.Invoke(newSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ActiveObjectEnumerator<T> GetEnumerator() => new(_entries.AsSpan(0, Count));
}