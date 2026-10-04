using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;

public static unsafe class NativeArray
{
    public static int AllocCount { get; private set; }
    public static long AllocSizeInBytes { get; private set; }

    public static float AllocSizeInMb => AllocSizeInBytes > 0 ? (float)(AllocSizeInBytes / 1024.0 / 1024.0) : 0;

    public static NativeArray<byte> Allocate(int capacity, bool zeroed = true) => Allocate<byte>(capacity, zeroed);

    public static NativeArray<T> Allocate<T>(int capacity, bool zeroed = true) where T : unmanaged
    {
        var ptr = AllocMemory(capacity, Unsafe.SizeOf<T>(), 0, zeroed);
        return new NativeArray<T>((T*)ptr, capacity, 0);
    }

    public static NativeArray<byte> AlignedAllocate(int capacity, int alignment, bool zeroed = true) =>
        AlignedAllocate<byte>(capacity, alignment, zeroed);

    public static NativeArray<T> AlignedAllocate<T>(int capacity, int alignment, bool zeroed = true)
        where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alignment);
        int alignedCapacity = IntMath.AlignUp(capacity, alignment);

        var ptr = AllocMemory(alignedCapacity, Unsafe.SizeOf<T>(), alignment, zeroed);
        return new NativeArray<T>((T*)ptr, alignedCapacity, alignment);
    }

    public static NativeArray<T> CreateFrom<T>(T* ptr, int length, int alignment = 0) where T : unmanaged
    {
        Validate(length, Unsafe.SizeOf<T>(), alignment);
        var array = new NativeArray<T>(ptr, length, alignment);
        AllocSizeInBytes += array.SizeInBytes;
        ++AllocCount;
        return array;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void* AllocMemory(int length, int stride, int alignment, bool zeroed)
    {
        Validate(length, stride, alignment);
        var bytes = (long)length * stride;
        AllocSizeInBytes += bytes;
        ++AllocCount;

        if (alignment > 0)
        {
            var ptr = NativeMemory.AlignedAlloc((nuint)bytes, (nuint)alignment);
            if (zeroed) NativeMemory.Clear(ptr, (nuint)bytes);
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
        
        var capacity = (long)length * stride;
        var newCapacity = (long)newLength * stride;
        var deltaBytes = newCapacity - capacity;
        
        if(deltaBytes < 0) Throwers.InvalidOperation();
        Validate((int)newCapacity, stride, alignment);

        ptr = alignment > 0
            ? NativeMemory.AlignedRealloc(ptr, (nuint)newCapacity, (nuint)alignment)
            : NativeMemory.Realloc(ptr, (nuint)newCapacity);

        if (zeroed && newCapacity > capacity)
        {
            NativeMemory.Clear((byte*)ptr + capacity, (nuint)deltaBytes);
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
        
        AllocSizeInBytes -= sizeInBytes;
        --AllocCount;
        
        if (AllocSizeInBytes < 0 || AllocCount < 0) Throwers.InvalidOperation();

/*
#if DEBUG
        Console.WriteLine($"Disposed {nameof(NativeArray)}: {capacity} bytes");
#endif
*/
    }


    [StackTraceHidden]
    private static void Validate(int length, int stride, int alignment)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 4);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stride);
        if(stride > 2 && IntMath.AlignUp(stride, 4) != stride) 
            Throwers.InvalidArgument(nameof(stride), $"Stride is not aligned {stride}");

        if (alignment == 0) return;

        ArgumentOutOfRangeException.ThrowIfLessThan(alignment, 16);
        ArgumentOutOfRangeException.ThrowIfEqual(IntMath.IsPowerOfTwo(alignment), false, nameof(alignment));
        ArgumentOutOfRangeException.ThrowIfNotEqual(length, IntMath.AlignUp(length, alignment));
    }
}