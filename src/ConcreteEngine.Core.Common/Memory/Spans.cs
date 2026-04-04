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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    public ref struct Enumerator(ZippedSpan<T1, T2> source)
    {
        private readonly ZippedSpan<T1, T2> _zip = source;
        private int _i = -1;
        private readonly int _length = source.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_i < _length;

        public readonly ref T1 Item1 => ref _zip.Span1[_i];
        public readonly ref T2 Item2 => ref _zip.Span2[_i];

    }
}

public ref struct SpanRange<T1, T2>(ReadOnlySpan<T1> range, Span<T2> dense)
    where T1 : unmanaged, IRange where T2 : unmanaged
{
    public ReadOnlySpan<T1> Range = range;
    public Span<T2> Dense = dense;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T2> GetSlice(int index)
    {
        if ((uint)index >= Range.Length) throw new IndexOutOfRangeException(nameof(index));
        var range = Range[index];
        return Dense.Slice(range.Offset, range.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Enumerator GetEnumerator() => new(this);

    public ref struct Enumerator(SpanRange<T1, T2> source)
    {
        private readonly SpanRange<T1, T2> _source = source;
        private int _i = -1;
        private readonly int _length = source.Range.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_i < _length;

        public readonly Span<T2> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source.GetSlice(_i);
        }
    }
}
