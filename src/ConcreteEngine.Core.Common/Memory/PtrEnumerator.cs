using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Memory;

public unsafe ref struct PtrEnumerator<T> where T : unmanaged
{
    private T* _p;
    private readonly T* _end;

    public PtrEnumerator(NativeView<T> view)
    {
        _p = view.Ptr - 1;
        _end = view.EndPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_p < _end;

    public readonly ref T Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref *_p;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly PtrEnumerator<T> GetEnumerator() => this;
}

public unsafe ref struct PtrEnumerator<T1, T2> where T1 : unmanaged where T2 : unmanaged
{
    private T1* _p1;
    private T2* _p2;
    private readonly T1* _end;

    public PtrEnumerator(NativeView<T1> p1, NativeView<T2> p2)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(p1.Length, p2.Length);
        _p1 = p1.Ptr - 1;
        _p2 = p2.Ptr - 1;
        _end = p1.EndPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        ++_p2;
        return ++_p1 < _end;
    }

    public readonly TupleRef<T1, T2> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref *_p1, ref *_p2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly PtrEnumerator<T1, T2> GetEnumerator() => this;
}

public unsafe ref struct PtrEnumerator<T1, T2, T3> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
{
    private T1* _p1;
    private T2* _p2;
    private T3* _p3;
    private readonly T1* _end;

    public PtrEnumerator(NativeView<T1> p1, NativeView<T2> p2, NativeView<T3> p3)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(p1.Length, p2.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(p1.Length, p3.Length);

        _p1 = p1.Ptr - 1;
        _p2 = p2.Ptr - 1;
        _p3 = p3.Ptr - 1;
        _end = p1.EndPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        ++_p2;
        ++_p3;
        return ++_p1 < _end;
    }

    public readonly TripleRef<T1, T2, T3> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref *_p1, ref *_p2, ref *_p3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly PtrEnumerator<T1, T2, T3> GetEnumerator() => this;
}