using System;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Identity;

// Test, might be crap idea

public readonly record struct Id<T>(int Value) : IComparable<Id<T>>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Value - 1;

    public int CompareTo(Id<T> other) => Value.CompareTo(other.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(Id<T> id) => id.Value;
}