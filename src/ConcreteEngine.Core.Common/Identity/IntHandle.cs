using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Identity;

public readonly record struct IntHandle(int Value, ushort Gen = 0) : IComparable<IntHandle>
{
    public int Index() => Value - 1;
    public int CompareTo(IntHandle other) => Value.CompareTo(other.Value);
    public static implicit operator int(IntHandle id) => id.Value;
}

public readonly record struct IntHandle<T>(int Value, ushort Gen = 0) : IComparable<IntHandle<T>>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Value - 1;

    public int CompareTo(IntHandle<T> other) => Value.CompareTo(other.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(IntHandle<T> id) => id.Value;
}