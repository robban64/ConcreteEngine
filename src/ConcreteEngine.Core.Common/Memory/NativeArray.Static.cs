using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;

public static unsafe class NativeArray
{
    public static ulong AllocSizeInBytes { get; private set; }
    public static int AllocCount { get; private set; }

    public static float AllocSizeInMb => AllocSizeInBytes > 0 ? AllocSizeInBytes / 1024.0f / 1024.0f : 0;

    public static NativeArray<byte> Allocate(int capacity, bool zeroed = true)
    {
        var ptr = AllocMemory(capacity, 1, 0, zeroed);
        return new NativeArray<byte>((byte*)ptr, capacity, 0);
    }

    public static NativeArray<T> Allocate<T>(int capacity, bool zeroed = true) where T : unmanaged
    {
        return new NativeArray<T>(AllocatePointer<T>(capacity, zeroed), capacity, 0);
    }

    public static NativeArray<T> AlignedAllocate<T>(int capacity, int alignment, bool zeroed = true)
        where T : unmanaged
    {
        var ptr = AllocMemory(capacity, Unsafe.SizeOf<T>(), alignment, zeroed);
        return new NativeArray<T>((T*)ptr, capacity, alignment);
    }

    public static T* AllocatePointer<T>(int capacity, bool zeroed = true) where T : unmanaged
    {
        var ptr = AllocMemory(capacity, Unsafe.SizeOf<T>(), 0, zeroed);
        return (T*)ptr;
    }

    public static T* ReAlloc<T>(T* ptr, int length, int newLength, int alignment, bool zeroed) where T : unmanaged
    {
        return (T*)ReAlloc(ptr, length, newLength, Unsafe.SizeOf<T>(), alignment, zeroed);
    }


    public static NativeArray<T> CreateFrom<T>(T* ptr, int length, int alignment = 0) where T : unmanaged
    {
        Validate(length, Unsafe.SizeOf<T>(), alignment);
        var array = new NativeArray<T>(ptr, length, alignment);
        AllocSizeInBytes += (ulong)array.SizeInBytes;
        ++AllocCount;
        return array;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void* AllocMemory(int length, int stride, int alignment, bool zeroed)
    {
        Validate(length, stride, alignment);

        var bytes = (nuint)length * (nuint)stride;
        AllocSizeInBytes += bytes;
        ++AllocCount;

        if (alignment > 0)
        {
            var ptr = NativeMemory.AlignedAlloc(bytes, (nuint)alignment);
            if (zeroed) NativeMemory.Clear(ptr, bytes);
            return ptr;
        }

        return zeroed
            ? NativeMemory.AllocZeroed((nuint)length, (nuint)stride)
            : NativeMemory.Alloc((nuint)length, (nuint)stride);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void* ReAlloc(void* ptr, int length, int newLength, int stride, int alignment,
        bool zeroed)
    {
        ArgumentNullException.ThrowIfNull(ptr);
        var capacity = (nuint)length * (nuint)stride;
        var newCapacity = (nuint)newLength * (nuint)stride;
        var deltaBytes = newCapacity - capacity;

        Validate((int)newCapacity, stride, alignment);

        ptr = alignment > 0
            ? NativeMemory.AlignedRealloc(ptr, newCapacity, (nuint)alignment)
            : NativeMemory.Realloc(ptr, newCapacity);

        if (zeroed && newCapacity > capacity)
        {
            NativeMemory.Clear((byte*)ptr + capacity, deltaBytes);
        }

        AllocSizeInBytes += deltaBytes;

#if DEBUG
        Console.WriteLine($"Reallocate {nameof(NativeArray)}: {newCapacity} bytes");
#endif
        return ptr;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void DisposeArray(void* ptr, int sizeInBytes, int alignment)
    {
        if (ptr == null) return;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeInBytes);

        if (alignment > 0) NativeMemory.AlignedFree(ptr);
        else NativeMemory.Free(ptr);

        if (AllocSizeInBytes - (ulong)sizeInBytes > AllocSizeInBytes)
            Throwers.InvalidOperation(nameof(AllocSizeInBytes));

        AllocSizeInBytes -= (ulong)sizeInBytes;
        --AllocCount;

/*
#if DEBUG
        Console.WriteLine($"Disposed {nameof(NativeArray)}: {capacity} bytes");
#endif
*/
    }


    [MethodImpl(MethodImplOptions.NoInlining), StackTraceHidden]
    private static void Validate(int length, int stride, int alignment)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 4);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stride);
        if (alignment != 0)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(alignment, 16);
            ArgumentOutOfRangeException.ThrowIfEqual(IntMath.IsPowerOfTwo(alignment), false, nameof(alignment));
        }
    }
}