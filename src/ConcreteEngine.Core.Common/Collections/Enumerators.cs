using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Identity;
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
    public readonly ActiveObjectEnumerator<T> GetEnumerator() => this;
}

public ref struct SparseObjectEnumerator<TId, TObj>(ReadOnlySpan<TId> idSpan, ReadOnlySpan<TObj?> objectSpan)
    where TId : unmanaged, ITypedId<TId> where TObj : class
{
    private int _i = -1;
    private int _currentIndex;
    private readonly ReadOnlySpan<TId> _idSpan = idSpan;
    private readonly ReadOnlySpan<TObj?> _objectSpan = objectSpan;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        while (++_i < _idSpan.Length)
        {
            _currentIndex = _idSpan[_i].Index;
            if ((uint)_currentIndex < (uint)_objectSpan.Length && _objectSpan[_currentIndex] != null)
                return true;
        }

        return false;
    }

    public readonly TObj Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _objectSpan[_currentIndex]!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly SparseObjectEnumerator<TId, TObj> GetEnumerator() => this;
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
    public readonly RefEnumerator<T> GetEnumerator() => this;
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

    public readonly ValueRef<T1, T2> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Unsafe.Add(ref _start1, _i), ref Unsafe.Add(ref _start2, _i));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ZipRefEnumerator<T1, T2> GetEnumerator() => this;
}



public ref struct ZipSpanEnumerator<T1, T2>
{
    private readonly Span<T1> _span1;
    private readonly Span<T2> _span2;
    private int _i = -1;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ZipSpanEnumerator<T1, T2> Make(Span<T1> span1,  Span<T2> span2) => new(span1, span2);

    public ZipSpanEnumerator(Span<T1> span1, Span<T2> span2)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(span1.Length, span2.Length);
        _span1 = span1;
        _span2 = span2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _span1.Length;

    public readonly ValueRef<T1, T2> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref _span1[_i], ref _span2[_i]);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ZipSpanEnumerator<T1,T2> GetEnumerator() => this;
        
}


public ref struct ZipSpanEnumerator<T1, T2, T3>
{
    private readonly Span<T1> _span1;
    private readonly Span<T2> _span2;
    private readonly Span<T3> _span3;

    private int _i = -1;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ZipSpanEnumerator<T1, T2, T3> Make(Span<T1> span1,  Span<T2> span2, Span<T3> span3) => new(span1, span2, span3);

    public ZipSpanEnumerator(Span<T1> span1, Span<T2> span2, Span<T3> span3)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(span1.Length, span2.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(span1.Length, span3.Length);
        _span1 = span1;
        _span2 = span2;
        _span3 = span3;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _span1.Length;

    public readonly ValueRef<T1, T2, T3> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref _span1[_i], ref _span2[_i], ref _span3[_i]);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ZipSpanEnumerator<T1,T2, T3> GetEnumerator() => this;
        
}