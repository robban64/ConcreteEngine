using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Memory;

public readonly ref struct UnsafeZippedSpan<T1, T2> where T1 : unmanaged where T2 : unmanaged
{
    private readonly ref T1 _start1;
    private readonly ref T2 _start2;
    public readonly int Length;

    public UnsafeZippedSpan(Span<T1> span1, Span<T2> span2)
    {
        if (span1.Length != span2.Length) throw new ArgumentException();
        _start1 = ref MemoryMarshal.GetReference(span1);
        _start2 = ref MemoryMarshal.GetReference(span2);
        Length = span1.Length;
    }

    public TuplePtr<T1, T2> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Unsafe.Add(ref _start1, index), ref Unsafe.Add(ref _start2, index));
    }
}

public readonly ref struct UnsafeSpan<T>(Span<T> span) where T : unmanaged
{
    public readonly Span<T> Span = span;
    private readonly ref T _start = ref MemoryMarshal.GetReference(span);
    public int Length => Span.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T At(int index) => ref Unsafe.Add(ref _start, index);
    
    public ValuePtr<T> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Unsafe.Add(ref _start, index));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DefaultEnumerator GetEnumerator() => new(this);
    
    public ref struct DefaultEnumerator(UnsafeSpan<T> span)
    {
        private readonly UnsafeSpan<T> _span = span;
        private int _i = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_i < _span.Length;

        public readonly ValuePtr<T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span[_i];
        }
    }
}

public readonly ref struct RefSlice<T> where T : unmanaged
{
    private readonly ref T _start;
    public readonly int Length;

    public RefSlice(ref T data0, int offset, int length)
    {
        _start = ref Unsafe.Add(ref data0, offset);
        Length = length;
    }

    public ref T this[int i]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(ref _start, i);
    }
}