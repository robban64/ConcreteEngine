using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;

public unsafe struct NativeArray<T> : IDisposable where T : unmanaged
{
    public T* Ptr;
    public readonly int Capacity;

    public NativeArray(int capacity, bool clear = true, int alignment = 16)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 4);
        ArgumentOutOfRangeException.ThrowIfLessThan(alignment, 16);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(alignment, 64);
        if (!IntMath.IsPowerOfTwo(alignment))
            throw new ArgumentOutOfRangeException($"{alignment} is not power of two", nameof(alignment));

        Capacity = capacity;

        var bytes = (nuint)(capacity * Unsafe.SizeOf<T>());

        Ptr = (T*)NativeMemory.AlignedAlloc(bytes, (nuint)alignment);

        if (clear) NativeMemory.Clear(Ptr, bytes);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(NativeArray<T> array) => array.Ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* operator +(NativeArray<T> a, int b) => a.Ptr + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* operator -(NativeArray<T> a, int b) => a.Ptr - b;


    public readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Ptr[index];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Ptr[index] = value;
    }

    public readonly void Clear()
    {
        var bytes = (nuint)(Capacity * Unsafe.SizeOf<T>());
        NativeMemory.Clear(Ptr, bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T GetRef(int index = 0) => ref Ptr[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ValuePtr<T> TryGet(int index)
    {
        return (uint)index < Capacity ? new ValuePtr<T>(ref Ptr[index]) : ValuePtr<T>.Null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan(int start = 0) => new(Ptr + start, Capacity - start);

    public readonly UnsafeSpanSlice<T> SpanSlice(int offset, int length)
    {
        if ((uint)offset + (uint)length > Capacity)
            throw new ArgumentOutOfRangeException($"Offset {offset} + length {length} is greater than {Capacity}");
        return new UnsafeSpanSlice<T>(ref Ptr[0], offset, length);
    }

    public void Dispose()
    {
        if (Ptr == null) return;

        NativeMemory.AlignedFree(Ptr);
        Ptr = null;
    }
}

/*
public unsafe struct NativeList<T> : IDisposable where T : unmanaged
{
    public T* Ptr;
    private int _capacity;
    private int _count;

    public NativeList(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 4);

        _capacity = capacity;
        _count = 0;
        Ptr = (T*)NativeMemory.AlignedAlloc((nuint)(capacity * Unsafe.SizeOf<T>()), 16);
    }

    public readonly ValuePtr<T> this[int index] => new(ref Ptr[index]);

    public readonly Span<T> AsSpan() => new(Ptr, _count);

    public void Add(in T item)
    {
        if (_count >= _capacity) Resize();
        Ptr[_count++] = item;
    }

    private void Resize()
    {
        var newCap = _capacity * 2;
        var newPtr = (T*)NativeMemory.AlignedRealloc(Ptr, (nuint)(newCap * sizeof(T)), 16);
        Ptr = newPtr;
        _capacity = newCap;
    }


    public void Dispose()
    {
        if (Ptr == null) return;

        NativeMemory.AlignedFree(Ptr);
        Ptr = null;
    }
}*/