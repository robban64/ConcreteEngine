using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Memory;

public readonly ref struct ValueRef<T>(ref T value) where T : unmanaged
{
    public readonly ref T Value = ref value;

    public bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.IsNullRef(ref Value);
    }

    public static ValueRef<T> Null
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Unsafe.NullRef<T>());
    }

    public static implicit operator bool(ValueRef<T> it) => !Unsafe.IsNullRef(ref it.Value);
}

public readonly ref struct TupleRef<T1, T2>(ref T1 it1, ref T2 it2) where T1 : unmanaged where T2 : unmanaged
{
    public readonly ref T1 Item1 = ref it1;
    public readonly ref T2 Item2 = ref it2;

    public bool AnyNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.IsNullRef(ref Item1) || Unsafe.IsNullRef(ref Item2);
    }

    public static TupleRef<T1, T2> Null
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Unsafe.NullRef<T1>(), ref Unsafe.NullRef<T2>());
    }
}

public readonly ref struct TripleRef<T1, T2, T3>(ref T1 it1, ref T2 it2, ref T3 it3)
    where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
{
    public readonly ref T1 Item1 = ref it1;
    public readonly ref T2 Item2 = ref it2;
    public readonly ref T3 Item3 = ref it3;

    public bool AnyNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.IsNullRef(in Item1) || Unsafe.IsNullRef(in Item2) || Unsafe.IsNullRef(in Item3);
    }

    public static TripleRef<T1, T2, T3> Null
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Unsafe.NullRef<T1>(), ref Unsafe.NullRef<T2>(), ref Unsafe.NullRef<T3>());
    }
}