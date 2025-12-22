using System.Runtime.CompilerServices;

namespace ConcreteEngine.Common.Generics;

public readonly record struct Slot<T>(int Index) : IComparable<Slot<T>>
{
    public readonly int Index = Index;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Index >= 0;

    public int CompareTo(Slot<T> other) => Index.CompareTo(other.Index);

    public static implicit operator int(Slot<T> id) => id.Index;
}