using System;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory.Enumerators;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Common.Memory;

public ref struct SpanRange<T1, T2>(ReadOnlySpan<T1> range, Span<T2> dense)
    where T1 : unmanaged, ISlotRange where T2 : unmanaged
{
    public ReadOnlySpan<T1> Range = range;
    public Span<T2> Dense = dense;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanSlice<T2> GetSlice(int index)
    {
        if ((uint)index >= Range.Length) throw new IndexOutOfRangeException(nameof(index));
        var range = Range[index];
        return new SpanSlice<T2>(Dense, range.Offset, range.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    public ref struct Enumerator(SpanRange<T1, T2> source)
    {
        private readonly ReadOnlySpan<T1> _ranges = source.Range;
        private readonly Span<T2> _dense = source.Dense;
        private int _i = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_i < _ranges.Length;

        public SpanSlice<T2> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ref readonly var range = ref _ranges[_i];
                return new SpanSlice<T2>(_dense, range.Offset, range.Length);
            }
        }
    }
}

public ref struct SpanSlice<T1>(Span<T1> span, int offset, int length) where T1 : unmanaged
{
    public Span<T1> Span = span.Slice(offset, length);
    public int Length => Span.Length;

    public ValuePtr<T1> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Span[index]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtrEnumerator<T1> GetEnumerator() => new(Span);
}

public readonly ref struct ZippedSpan<T1, T2> where T1 : unmanaged where T2 : unmanaged
{
    private readonly Span<T1> _span1;
    private readonly Span<T2> _span2;
    public int Length => _span1.Length;

    public ZippedSpan(Span<T1> span1, Span<T2> span2)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(span1.Length, span2.Length);
        _span1 = span1;
        _span2 = span2;
    }

    public TuplePtr<T1, T2> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref _span1[index], ref _span2[index]);
    }
}
/*
public readonly ref struct SpanSlice<T1, T2, T3> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
{
    public readonly Span<T1> _span1;
    public readonly Span<T2> _span2;
    public readonly Span<T3> _span3;

    public SpanSlice(Span<T1> span1, Span<T2> span2, Span<T3> span3, int offset, int length)
    {
        if (span1.Length != span2.Length || span1.Length != span3.Length)
            throw new ArgumentOutOfRangeException();

        _span1 = span1.Slice(offset, length);
        _span2 = span2.Slice(offset, length);
        _span3 = span3.Slice(offset, length);
    }

    public int Length => _span1.Length;

    public TriplePtr<T1, T2, T3> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref _span1[index], ref _span2[index], ref _span3[index]);
    }
}

public readonly ref struct ZippedSpan<T1, T2, T3> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
{
    private readonly Span<T1> _span1;
    private readonly Span<T2> _span2;
    private readonly Span<T3> _span3;

    public int Length => _span1.Length;

    public ZippedSpan(Span<T1> span1, Span<T2> span2, Span<T3> span3)
    {
        if (span1.Length != span2.Length || span1.Length != span3.Length)
            throw new ArgumentOutOfRangeException();

        _span1 = span1;
        _span2 = span2;
        _span3 = span3;
    }

    public TriplePtr<T1, T2, T3> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref _span1[index], ref _span2[index], ref _span3[index]);
    }
}*/