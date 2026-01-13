using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Identity;

public readonly record struct IntHandle<T>(int Value, ushort Gen = 0) : IComparable<IntHandle<T>>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Value - 1;

    public int CompareTo(IntHandle<T> other) => Value.CompareTo(other.Value);

    public static implicit operator int(IntHandle<T> id) => id.Value;
}

public readonly record struct ShortHandle<T> : IComparable<ShortHandle<T>>
{
    public readonly ushort Value;
    public readonly ushort Gen;

    public ShortHandle(ushort value, ushort gen)
    {
        Value = value;
        Gen = gen;
    }

    public ShortHandle(int value, int gen)
    {
        Value = (ushort)value;
        Gen = (ushort)gen;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Value - 1;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Value > 0 && Gen > 0;

    public static implicit operator int(ShortHandle<T> handle) => handle.Value;

    public int CompareTo(ShortHandle<T> other) => Value.CompareTo(other.Value);

    public static ShortHandle<T> Empty = default;
}