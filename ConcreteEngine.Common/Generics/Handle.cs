using System.Runtime.CompilerServices;

namespace ConcreteEngine.Common.Generics;

public readonly record struct Handle<T>(int Id) : IComparable<Handle<T>>
{
    public readonly int Id = Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Id > 0;

    public int CompareTo(Handle<T> other) => Id.CompareTo(other.Id);


    public static implicit operator int(Handle<T> id) => id.Id;
}