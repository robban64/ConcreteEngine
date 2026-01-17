using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Common.Memory;

public readonly ref struct ZippedSpan<T1, T2>
{
    public readonly Span<T1> Span1;
    public readonly Span<T2> Span2;
    public int Length => Span1.Length;

    public ZippedSpan(Span<T1> span1, Span<T2> span2)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(span1.Length, span2.Length);
        Span1 = span1;
        Span2 = span2;
    }

    public readonly ref struct IterView(int index, T1 t1, T2 t2)
    {
        public readonly T1 Item1 = t1;
        public readonly T2 Item2 = t2;
        public readonly int Index = index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    public ref struct Enumerator(ZippedSpan<T1, T2> source)
    {
        private readonly Span<T1> _s1 = source.Span1;
        private readonly Span<T2> _s2 = source.Span2;
        private int _i = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_i < _s1.Length;

        public IterView Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_i, _s1[_i], _s2[_i]);
        }
    }
}

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtr<T1> At(int index) => new(ref Span[index]);

    public ref T1 this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Span[index];
    }
}