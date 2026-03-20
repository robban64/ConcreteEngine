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
        : this(ref MemoryMarshal.GetReference(span1), ref MemoryMarshal.GetReference(span2), span1.Length)
    {
        if (span1.Length != span2.Length) throw new ArgumentException();
    }

    public UnsafeZippedSpan(ref T1 start1, ref T2 start2, int length)
    {
        _start1 = ref start1;
        _start2 = ref start2;
        Length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T1 At1(int index) => ref Unsafe.Add(ref _start1, index);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T2 At2(int index) => ref Unsafe.Add(ref _start2, index);

    public TuplePtr<T1, T2> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Unsafe.Add(ref _start1, index), ref Unsafe.Add(ref _start2, index));
    }

    public TuplePtrEnumerator<T1, T2> GetEnumerator() => new(ref _start1, ref _start2, Length);
}

public readonly ref struct UnsafeSpan<T> where T : unmanaged
{
    public readonly ref T Start;
    public readonly int Length;

    public UnsafeSpan(ref T start, int length)
    {
        Length = length;
        Start = ref start;
    }

    public unsafe UnsafeSpan(T* ptr, int length) : this(ref *ptr, length)
    {
    }

    public UnsafeSpan(Span<T> span) : this(ref MemoryMarshal.GetReference(span), span.Length)
    {
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtr<T> At(int index) => new(ref Unsafe.Add(ref Start, index));

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(ref Start, index);
    }

    public ValuePtrEnumerator<T> GetEnumerator() => new(ref Start, Length);
}

public readonly ref struct UnsafeSpanSlice<T> where T : unmanaged
{
    private readonly ref T _start;
    public readonly int Length;

    public UnsafeSpanSlice(ref T start, int offset, int length)
    {
        _start = ref Unsafe.Add(ref start, offset);
        Length = length;
    }

    public UnsafeSpanSlice(Span<T> span, int offset, int length) : this(
        ref MemoryMarshal.GetReference(span), offset, length)
    {
    }

    public ref T this[int i]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(ref _start, i);
    }

    public ValuePtrEnumerator<T> GetEnumerator() => new(ref _start, Length);
}