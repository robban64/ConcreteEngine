using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Memory.Enumerators;

public ref struct ValuePtrEnumerator<T> where T : unmanaged
{
    private readonly ref T _value;
    private readonly int _length;

    private int _i = -1;

    public ValuePtrEnumerator(Span<T> span)
    {
        _value = ref MemoryMarshal.GetReference(span);
        _length = span.Length;
    }
    
    public ValuePtrEnumerator(ref T start, int length)
    {
        _value = ref start;
        _length = length;
    }
    
    public unsafe ValuePtrEnumerator(T* start, int length)
    {
        _value = ref Unsafe.AsRef<T>(start);
        _length = length;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _length;

    public readonly ValuePtr<T> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Unsafe.Add(ref _value, _i));
    }
}

public ref struct TuplePtrEnumerator<T1, T2> where T1 : unmanaged where T2 : unmanaged
{
    private readonly Span<T1> _span1;
    private readonly Span<T2> _span2;
    private int _i = -1;

    public readonly int Length => _span1.Length;

    public TuplePtrEnumerator(Span<T1> span1, Span<T2> span2)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(span1.Length, span2.Length);
        _span1 = span1;
        _span2 = span2;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _span1.Length;

    public readonly ValuePtr<T1> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref _span1[_i]);
    }
}

public ref struct TriplePtrEnumerator<T1, T2, T3> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
{
    private readonly Span<T1> _span1;
    private readonly Span<T2> _span2;
    private readonly Span<T3> _span3;
    private int _i = -1;

    public int Length => _span1.Length;

    public TriplePtrEnumerator(Span<T1> span1, Span<T2> span2, Span<T3> span3)
    {
        if (span1.Length != span2.Length || span1.Length != span3.Length)
            throw new ArgumentOutOfRangeException();

        _span1 = span1;
        _span2 = span2;
        _span3 = span3;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _span1.Length;

    public readonly TriplePtr<T1, T2, T3> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref _span1[_i], ref _span2[_i], ref _span3[_i]);
    }
}