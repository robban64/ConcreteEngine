using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory.Enumerators;

namespace ConcreteEngine.Core.Common.Memory;

public readonly ref struct UnsafeZippedSpan<T1, T2> where T1 : unmanaged where T2 : unmanaged
{
    public readonly ref T1 Ref1;
    public readonly ref T2 Ref2;
    public readonly int Length;

    public UnsafeZippedSpan(Span<T1> span1, Span<T2> span2)
        : this(ref MemoryMarshal.GetReference(span1), ref MemoryMarshal.GetReference(span2), span1.Length)
    {
        if (span1.Length != span2.Length) throw new ArgumentException();
    }

    public UnsafeZippedSpan(ref T1 start1, ref T2 start2, int length)
    {
        Ref1 = ref start1;
        Ref2 = ref start2;
        Length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T1 At1(int index) => ref Unsafe.Add(ref Ref1, index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T2 At2(int index) => ref Unsafe.Add(ref Ref2, index);

    public TuplePtr<T1, T2> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Unsafe.Add(ref Ref1, index), ref Unsafe.Add(ref Ref2, index));
    }

    public TuplePtrEnumerator<T1, T2> GetEnumerator() => new(ref Ref1, ref Ref2, Length);
}

public readonly ref struct UnsafeSpan<T> where T : unmanaged
{
    public readonly ref T Ref;
    public readonly int Length;

    public UnsafeSpan(ref T start, int length)
    {
        Length = length;
        Ref = ref start;
    }

    public unsafe UnsafeSpan(T* ptr, int length) : this(ref *ptr, length) { }
    public UnsafeSpan(Span<T> span) : this(ref MemoryMarshal.GetReference(span), span.Length) { }
    public UnsafeSpan(ReadOnlySpan<T> span) : this(ref MemoryMarshal.GetReference(span), span.Length) { }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtr<T> At(int index) => new(ref Unsafe.Add(ref Ref, index));

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(ref Ref, index);
    }

    public ValuePtrEnumerator<T> GetEnumerator() => new(ref Ref, Length);
}