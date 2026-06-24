using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common;

//?
public readonly record struct Id16<T>(ushort Value) : IComparable<ushort>, IComparable<Id16<T>> where T : class
{
    public Id16(int value) : this((ushort)value) { }

    public readonly ushort Value = Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Value - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Value > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ushort(Id16<T> slot) => slot.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Id16<T>(ushort i) => new(i);

    public int CompareTo(ushort other) => Value.CompareTo(other);
    public int CompareTo(Id16<T> other) => Value.CompareTo(other.Value);

    public static readonly Id16<T> Empty = default;
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Id32<T>(int Value) : IComparable<int>, IComparable<Id32<T>> where T : class
{
    public readonly int Value = Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Value - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Value > 0;

    public static implicit operator int(Id32<T> id) => id.Value;
    public static explicit operator Id32<T>(int i) => new(i);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Id32<T> a, int b) => a.Value > b && a.Value > b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Id32<T> a, int b) => a.Value < b && a.Value < b;


    public int CompareTo(int other) => Value.CompareTo(other);
    public int CompareTo(Id32<T> other) => Value.CompareTo(other.Value);

    public static readonly Id32<T> Empty = default;
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Handle32<T>(int Id, ushort Gen)
    : IComparable<int>, IComparable<Handle32<T>> where T : class
{
    public Handle32(int id, int gen) : this(id, (ushort)gen) { }

    public readonly int Id = Id;
    public readonly ushort Gen = Gen;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Id > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(Handle32<T> id) => id.Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Handle32<T> a, int b) => a.Id > b && a.Id > b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Handle32<T> a, int b) => a.Id < b && a.Id < b;

    public int CompareTo(int other) => Id.CompareTo(other);
    public int CompareTo(Handle32<T> other) => Id.CompareTo(other.Id);

    public static readonly Handle32<T> Empty = default;
}