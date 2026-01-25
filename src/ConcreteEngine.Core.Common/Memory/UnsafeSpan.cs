using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory.Enumerators;

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

public readonly ref struct UnsafeSpan<T> where T : unmanaged
{
    private readonly ref T _start;
    private readonly int _length;

    public UnsafeSpan(Span<T> span)
    {
        _length = span.Length;
        _start = ref MemoryMarshal.GetReference(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtr<T> At(int index) => new(ref Unsafe.Add(ref _start, index));

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(ref _start, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtrEnumerator<T> GetEnumerator() => new(ref _start, _length);

    public ref struct DefaultEnumerator(UnsafeSpan<T> span)
    {
        private readonly UnsafeSpan<T> _span = span;
        private int _i = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_i < _span._length;

        public readonly ValuePtr<T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span.At(_i);
        }
    }
}

public readonly ref struct RefSlice<T> where T : unmanaged
{
    private readonly ref T _start;
    public readonly int Length;

    public unsafe RefSlice(T* start, int offset, int length)
    {
        _start = ref Unsafe.Add(ref Unsafe.AsRef<T>(start), offset);
        Length = length;
    }

    public RefSlice(ref T start, int offset, int length)
    {
        _start = ref Unsafe.Add(ref start, offset);
        Length = length;
    }

    public ref T this[int i]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(ref _start, i);
    }
}