using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Common.Collections;

public ref struct ActiveObjectEnumerator<T>(ReadOnlySpan<T?> span) where T : class
{
    private readonly ReadOnlySpan<T?> _span = span;
    private int _i = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        while (++_i < _span.Length)
        {
            if (_span[_i] != null) return true;
        }

        return false;
    }

    public readonly T Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _span[_i]!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ActiveObjectEnumerator<T> GetEnumerator() => new (_span);
}

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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly RefEnumerator<T> GetEnumerator() => new (ref _start, _length);

}

public ref struct ZipRefEnumerator<T1, T2> where T1 : unmanaged where T2 : unmanaged
{
    private readonly ref T1 _start1;
    private readonly ref T2 _start2;
    private readonly int _length;
    private int _i = -1;

    public ZipRefEnumerator(ref T1 start1, ref T2 start2, int length)
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ZipRefEnumerator<T1,T2> GetEnumerator() => new (ref _start1, ref _start2, _length);

}