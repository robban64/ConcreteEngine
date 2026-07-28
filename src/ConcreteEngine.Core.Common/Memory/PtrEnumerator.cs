using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Memory;

public unsafe ref struct PtrEnumerator<T> where T : unmanaged
{
    private T* _p;
    private readonly T* _end;

    public PtrEnumerator(T* start, int length)
    {
        _p = start - 1;
        _end = start + length;
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

    public PtrEnumerator(T1* p1, T2* p2, int length)
    {
        _p1 = p1 - 1;
        _p2 = p2 - 1;
        _end = p1 + length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        ++_p2;
        return ++_p1 < _end;
    }

    public readonly TuplePtr<T1, T2> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref *_p1, ref *_p2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly PtrEnumerator<T1, T2> GetEnumerator() => this;
}