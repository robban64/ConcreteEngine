using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Memory;

public ref struct RefEnumerator<T> where T : unmanaged
{
    private readonly ref T _start;
    private readonly int _length;
    private int _i = -1;

    public RefEnumerator(ref T start, int length)
    {
        _start = ref start;
        _length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _length;

    public readonly ref T Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(ref _start, _i);
    }
}


public ref struct ValuePtrEnumerator<T> where T : unmanaged
{
    private readonly ref T _start;
    private readonly int _length;
    private int _i = -1;

    public ValuePtrEnumerator(ref T start, int length)
    {
        _start = ref start;
        _length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _length;

    public readonly ValuePtr<T> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Unsafe.Add(ref _start, _i));
    }
}

public ref struct TuplePtrEnumerator<T1, T2> where T1 : unmanaged where T2 : unmanaged
{
    private readonly ref T1 _start1;
    private readonly ref T2 _start2;
    private readonly int _length;
    private int _i = -1;

    public TuplePtrEnumerator(ref T1 start1, ref T2 start2, int length)
    {
        _start1 = ref start1;
        _start2 = ref start2;
        _length = length;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _length;

    public readonly TuplePtr<T1, T2> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Unsafe.Add(ref _start1, _i), ref Unsafe.Add(ref _start2, _i));
    }
}