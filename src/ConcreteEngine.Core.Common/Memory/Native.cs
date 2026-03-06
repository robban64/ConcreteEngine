using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;

public static class NativeArray
{
    [MethodImpl(MethodImplOptions.NoInlining), StackTraceHidden]
    public static void Validate(int capacity, int alignment)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 4);
        ArgumentOutOfRangeException.ThrowIfLessThan(alignment, 16);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(alignment, 64);
        if (!IntMath.IsPowerOfTwo(alignment))
            throw new ArgumentOutOfRangeException($"{alignment} is not power of two", nameof(alignment));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static NativeArray<T> Allocate<T>(int capacity, bool clear = true, int alignment = 16) where T : unmanaged
    {
        return new NativeArray<T>(capacity, clear, alignment);
    }
}

public unsafe struct NativeArray<T> : IDisposable where T : unmanaged
{
    public T* Ptr;
    public int Capacity;
    public readonly int Alignment;

    internal NativeArray(int capacity, bool clear = true, int alignment = 16)
    {
        NativeArray.Validate(capacity, alignment);

        var bytes = (nuint)capacity * (nuint)Unsafe.SizeOf<T>();
        Ptr = (T*)NativeMemory.AlignedAlloc(bytes, (nuint)alignment);
        Capacity = capacity;
        Alignment = alignment;

        if (clear) NativeMemory.Clear(Ptr, bytes);
    }
    
    public readonly bool IsNull => Ptr == null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(NativeArray<T> array) => array.Ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* operator +(NativeArray<T> a, int b) => a.Ptr + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* operator -(NativeArray<T> a, int b) => a.Ptr - b;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Ptr[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ValuePtr<T> TryGet(int index) =>
        (uint)index < (uint)Capacity ? new ValuePtr<T>(ref Ptr[index]) : ValuePtr<T>.Null;


    public readonly Span<T> AsSpan(int start = 0, int length = -1) =>
        new(Ptr + start, length < 0 ? Capacity - start : length);

    public readonly void Clear()
    {
        var bytes = (nuint)(Capacity * Unsafe.SizeOf<T>());
        NativeMemory.Clear(Ptr, bytes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Resize(int newCapacity, bool clear = true)
    {
        NativeArray.Validate(newCapacity, Alignment);
        var bytes = (nuint)newCapacity * (nuint)Unsafe.SizeOf<T>();
        var newPtr = (T*)NativeMemory.AlignedRealloc(Ptr, bytes, (nuint)Alignment);
        Ptr = newPtr;
        Capacity = newCapacity;

        if (clear) NativeMemory.Clear(Ptr, bytes);
        
        Console.WriteLine($"Reallocate {nameof(NativeArray)}: {bytes} bytes");
    }

    public void Dispose()
    {
        if (Ptr == null) return;

        NativeMemory.AlignedFree(Ptr);
        Ptr = null;

        var bytes = (nuint)Capacity * (nuint)Unsafe.SizeOf<T>();
        Console.WriteLine($"Disposed {nameof(NativeArray)}: {Capacity} bytes");
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