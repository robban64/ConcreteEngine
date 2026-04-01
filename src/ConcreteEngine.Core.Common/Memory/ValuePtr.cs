using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Memory;

public readonly ref struct ValuePtr<T>(ref T value) where T : unmanaged
{
    public readonly ref T Value = ref value;

    public bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.IsNullRef(ref Value);
    }

    public static ValuePtr<T> Null
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Unsafe.NullRef<T>());
    }

    public static implicit operator bool(ValuePtr<T> ptr) => !Unsafe.IsNullRef(ref ptr.Value);

}

public readonly ref struct TuplePtr<T1, T2>(ref T1 v1, ref T2 v2) where T1 : unmanaged where T2 : unmanaged
{
    public readonly ref T1 Item1 = ref v1;
    public readonly ref T2 Item2 = ref v2;

    public bool AnyNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.IsNullRef(in Item1) || Unsafe.IsNullRef(in Item2);
    }

    public static TuplePtr<T1, T2> Null
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Unsafe.NullRef<T1>(), ref Unsafe.NullRef<T2>());
    }
}
/*
public readonly ref struct TriplePtr<T1, T2, T3>(ref T1 v1, ref T2 v2, ref T3 v3)
    where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
{
    private readonly ref T1 _item1 = ref v1;
    private readonly ref T2 _item2 = ref v2;
    private readonly ref T3 _item3 = ref v3;


    public ref T1 Item1 => ref _item1;
    public ref T2 Item2 => ref _item2;
    public ref T3 Item3 => ref _item3;

    public bool AnyNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.IsNullRef(in _item1) || Unsafe.IsNullRef(in _item2) || Unsafe.IsNullRef(in _item3);
    }

    public static TriplePtr<T1, T2, T3> Null
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Unsafe.NullRef<T1>(), ref Unsafe.NullRef<T2>(), ref Unsafe.NullRef<T3>());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowNullRef() => throw new NullReferenceException("TriplePtr is null");
}
*/