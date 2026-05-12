using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Identity;

public readonly record struct Id32<T>(int Value): IComparable<int>, IComparable<Id32<T>> where T : class
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Value - 1;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Value > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(Id32<T> id) => id.Value;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(int other) => Value.CompareTo(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Id32<T> other) => Value.CompareTo(other.Value);
}

public readonly record struct IdGen32<T>(int Value, ushort Gen = 0) where T : class
{
    public int Index() => Value - 1;
    public static implicit operator int(IdGen32<T> id) => id.Value;
}