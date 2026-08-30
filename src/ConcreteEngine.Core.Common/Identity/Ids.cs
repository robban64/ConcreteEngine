using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Identity;

public readonly record struct Id16<T>(ushort Id)
    : ITypedId<Id16<T>>, IComparable<ushort>, IComparable<Id16<T>>
{
    public Id16(int value) : this((ushort)value) { }

    public readonly ushort Id = Id;

    public int Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id - 1;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ushort(Id16<T> slot) => slot.Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Id16<T>(ushort i) => new(i);

    public int CompareTo(ushort other) => Id.CompareTo(other);
    public int CompareTo(Id16<T> other) => Id.CompareTo(other.Id);

    public static readonly Id16<T> Empty = default;
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Id32<T>(int Id)
    : ITypedId<Id32<T>>, IComparable<int>, IComparable<Id32<T>>
{
    public int Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id - 1;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id > 0;
    }

    public static implicit operator int(Id32<T> id) => id.Id;
    public static explicit operator Id32<T>(int i) => new(i);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Id32<T> a, int b) => a.Id > b && a.Id > b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Id32<T> a, int b) => a.Id < b && a.Id < b;


    public int CompareTo(int other) => Id.CompareTo(other);
    public int CompareTo(Id32<T> other) => Id.CompareTo(other.Id);

    public static readonly Id32<T> Empty = default;
}