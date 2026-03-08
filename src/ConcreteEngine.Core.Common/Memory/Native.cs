using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;

public static class NativeArray
{
    [StackTraceHidden]
    public static void Validate(int capacity, int alignment)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 4);
        if (alignment != 0)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(alignment, 16);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(alignment, 64);
            if (!IntMath.IsPowerOfTwo(alignment))
                throw new ArgumentOutOfRangeException($"{alignment} is not power of two", nameof(alignment));
        }
    }

    public static NativeArray<T> Allocate<T>(int capacity, bool zeroed = true) where T : unmanaged
    {
        return new NativeArray<T>(capacity, 0, zeroed);
    }

    public static NativeArray<T> AlignedAllocate<T>(int capacity, int alignment = 16, bool zeroed = true)
        where T : unmanaged
    {
        return new NativeArray<T>(capacity, alignment, zeroed);
    }
}

public unsafe struct NativeArray<T> : IDisposable where T : unmanaged
{
    public T* Ptr;
    public int Length;
    public readonly int Alignment;

    internal NativeArray(int length, int alignment, bool zeroed)
    {
        NativeArray.Validate(length, alignment);

        if (alignment > 0)
        {
            var bytes = (nuint)length * (nuint)Unsafe.SizeOf<T>();
            Ptr = (T*)NativeMemory.AlignedAlloc(bytes, (nuint)alignment);
            if (zeroed) NativeMemory.Clear(Ptr, bytes);
        }
        else
        {
            Ptr = zeroed
                ? (T*)NativeMemory.AllocZeroed((nuint)length, (nuint)Unsafe.SizeOf<T>())
                : (T*)NativeMemory.Alloc((nuint)length, (nuint)Unsafe.SizeOf<T>());
        }

        Length = length;
        Alignment = alignment;
    }

    public readonly bool IsNull => Ptr == null;
    public readonly int SizeInBytes => Length * Unsafe.SizeOf<T>();

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
        (uint)index < (uint)Length ? new ValuePtr<T>(ref Ptr[index]) : ValuePtr<T>.Null;


    public readonly Span<T> AsSpan(int start = 0, int length = -1) =>
        new(Ptr + start, length < 0 ? Length - start : length);

    public readonly void Clear()
    {
        NativeMemory.Clear(Ptr, (nuint)SizeInBytes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Resize(int newCapacity, bool zeroed)
    {
        NativeArray.Validate(newCapacity, Alignment);
        var oldCapacity = Length;
        var bytes = (nuint)newCapacity * (nuint)Unsafe.SizeOf<T>();

        Ptr = Alignment > 0
            ? (T*)NativeMemory.AlignedRealloc(Ptr, bytes, (nuint)Alignment)
            : (T*)NativeMemory.Realloc(Ptr, bytes);

        Length = newCapacity;

        if (zeroed && newCapacity > oldCapacity)
        {
            var clearBytes = (nuint)(newCapacity - oldCapacity) * (nuint)Unsafe.SizeOf<T>();
            NativeMemory.Clear(Ptr + oldCapacity, clearBytes);
        }

        Console.WriteLine($"Reallocate {nameof(NativeArray)}: {bytes} bytes");
    }

    public void Dispose()
    {
        if (Ptr == null) return;

        if (Alignment > 0)
        {
            NativeMemory.AlignedFree(Ptr);
        }
        else
        {
            NativeMemory.Free(Ptr);
        }

        Ptr = null;
        Console.WriteLine($"Disposed {nameof(NativeArray)}: {SizeInBytes} bytes");
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