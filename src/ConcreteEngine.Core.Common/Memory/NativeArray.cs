using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;

public static unsafe class NativeArray
{
    public static Pointer<T> AllocatePtr<T>(int length, bool zeroed = true) where T : unmanaged
    {
        var ptr  = (T*)AllocMemory(length, Unsafe.SizeOf<T>(), 0, zeroed);
        return new Pointer<T>(ptr);
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

    [MethodImpl(MethodImplOptions.NoInlining), StackTraceHidden]
    internal static void Validate(int capacity, int alignment)
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


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static unsafe void* AllocMemory(int length, int stride, int alignment, bool zeroed)
    {
        Validate(length, alignment);

        if (alignment > 0)
        {
            var bytes = (nuint)length * (nuint)stride;
            var ptr = NativeMemory.AlignedAlloc(bytes, (nuint)alignment);
            if (zeroed) NativeMemory.Clear(ptr, bytes);
            return ptr;
        }

        return zeroed
            ? NativeMemory.AllocZeroed((nuint)length, (nuint)stride)
            : NativeMemory.Alloc((nuint)length, (nuint)stride);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static unsafe void* Resize(void* ptr, int length, int newLength, int stride, int alignment,
        bool zeroed)
    {
        var capacity = (nuint)length * (nuint)stride;
        var newCapacity = (nuint)newLength * (nuint)stride;

        Validate((int)newCapacity, alignment);

        ptr = alignment > 0
            ? NativeMemory.AlignedRealloc(ptr, newCapacity, (nuint)alignment)
            : NativeMemory.Realloc(ptr, newCapacity);

        if (zeroed && newCapacity > capacity)
        {
            var clearBytes = newCapacity - capacity;
            NativeMemory.Clear((byte*)ptr + capacity, clearBytes);
        }
#if DEBUG
        Console.WriteLine($"Reallocate {nameof(NativeArray)}: {newCapacity} bytes");
#endif
        return ptr;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static unsafe void DisposeArray(void* ptr, int capacity, int alignment)
    {
        if (ptr == null) return;

        if (alignment > 0) NativeMemory.AlignedFree(ptr);
        else NativeMemory.Free(ptr);

#if DEBUG
        Console.WriteLine($"Disposed {nameof(NativeArray)}: {capacity} bytes");
#endif
    }
}

public unsafe struct NativeArray<T> : IDisposable where T : unmanaged
{
    public T* Ptr;
    public int Length;
    public readonly int Alignment;

    internal NativeArray(int length, int alignment, bool zeroed)
    {
        Ptr = (T*)NativeArray.AllocMemory(length, Unsafe.SizeOf<T>(), alignment, zeroed);
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

    public readonly void Clear() => NativeMemory.Clear(Ptr, (nuint)SizeInBytes);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Resize(int newLength, bool zeroed)
    {
        Ptr = (T*)NativeArray.Resize(Ptr, Length, newLength, Unsafe.SizeOf<T>(), Alignment, zeroed);
        Length = newLength;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Dispose()
    {
        NativeArray.DisposeArray(Ptr, SizeInBytes, Alignment);
        Ptr = null;
        Length = 0;
    }
}