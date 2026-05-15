using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common;

//?
public readonly record struct Slot16<T>(ushort Value) where T : class
{
    public readonly ushort Value = Value;
    
    public static implicit operator int(Slot16<T> slot) => slot.Value;
    public static explicit operator Slot16<T>(ushort i) => new(i);
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Id32<T>(int Value) : IComparable<int>, IComparable<Id32<T>> where T : class
{
    public readonly int Value = Value;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Value - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Value > 0;

    public static explicit operator Id32<T>(int i) => new(i);
    public static explicit operator int(Id32<T> id) => id.Value;

    public int CompareTo(int other) => Value.CompareTo(other);
    public int CompareTo(Id32<T> other) => Value.CompareTo(other.Value);

    public static readonly Id32<T> Empty = default;
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Handle16<T>(ushort Value, ushort Gen)
    : IComparable<int>, IComparable<Handle16<T>> where T : class
{
    public Handle16(int id, int gen): this((ushort)id, (ushort)gen){}

    public readonly ushort Value = Value;
    public readonly ushort Gen = Gen;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Value - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Value > 0;

    public int CompareTo(int other) => Value.CompareTo((ushort)other);
    public int CompareTo(Handle16<T> other) => Value.CompareTo(other.Value);
    
    public static readonly Handle16<T> Empty = default;
}