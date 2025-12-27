using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Identity;

// Test, might be crap idea

public readonly record struct SlotIndex(int Index) : IComparable<SlotIndex>
{
    public int CompareTo(SlotIndex other) => Index.CompareTo(other.Index);
    public static implicit operator int(SlotIndex id) => id.Index;
}

public readonly record struct SlotIndex<T>(int Index) : IComparable<SlotIndex<T>>
{
    public int CompareTo(SlotIndex<T> other) => Index.CompareTo(other.Index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(SlotIndex<T> id) => id.Index;

    public static implicit operator SlotIndex(SlotIndex<T> id) => new(id);
}