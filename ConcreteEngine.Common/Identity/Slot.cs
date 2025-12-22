using System.Runtime.CompilerServices;

namespace ConcreteEngine.Common.Identity;


public readonly record struct Slot(int Index) : IComparable<Slot>
{
    public int CompareTo(Slot other) => Index.CompareTo(other.Index);
    public static implicit operator int(Slot id) => id.Index;
    
}

public readonly record struct Slot<T>(int Index) : IComparable<Slot<T>>
{
    public int CompareTo(Slot<T> other) => Index.CompareTo(other.Index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(Slot<T> id) => id.Index;
    public static implicit operator Slot(Slot<T> id) => new (id);
}