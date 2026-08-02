using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;


public static unsafe class NativeArray
{
    public static NativeArray<T> CreateFrom<T>(T* ptr, int length, int alignment = 0) where T : unmanaged
    {
        Validate(length, Unsafe.SizeOf<T>(), alignment);
        return new NativeArray<T>(ptr, length, alignment);
    }
    
    public static NativeArray<byte> Allocate(int capacity, bool zeroed = true) 
    {
        var ptr = AllocMemory(capacity, 1, 0, zeroed);
        return new NativeArray<byte>((byte*)ptr, capacity, 0);
    }

    public static NativeArray<T> Allocate<T>(int capacity, bool zeroed = true) where T : unmanaged
    {
        return new NativeArray<T>(AllocatePointer<T>(capacity, zeroed), capacity, 0);
    }
    
    public static NativeArray<T> AlignedAllocate<T>(int capacity, int alignment = 16, bool zeroed = true)
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
    
    public static T* Resize<T>(T* ptr, int length, int newLength, int alignment, bool zeroed) where T : unmanaged
    {
        return (T*)Resize(ptr, length, newLength, Unsafe.SizeOf<T>(), alignment, zeroed);
    }
    

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void* Resize(void* ptr, int length, int newLength, int stride, int alignment,
        bool zeroed)
    {
        ArgumentNullException.ThrowIfNull(ptr);
        var capacity = (nuint)length * (nuint)stride;
        var newCapacity = (nuint)newLength * (nuint)stride;

        Validate((int)newCapacity, stride, alignment);

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
    public static void DisposeArray(void* ptr, int alignment)
    {
        if (ptr == null) return;

        if (alignment > 0) NativeMemory.AlignedFree(ptr);
        else NativeMemory.Free(ptr);
/*
#if DEBUG
        Console.WriteLine($"Disposed {nameof(NativeArray)}: {capacity} bytes");
#endif
*/
    }
    
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void* AllocMemory(int length, int stride, int alignment, bool zeroed)
    {
        Validate(length, stride, alignment);

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
    
    [MethodImpl(MethodImplOptions.NoInlining), StackTraceHidden]
    private static void Validate(int capacity, int stride, int alignment)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 4);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stride);
        if (alignment != 0)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(alignment, 16);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(alignment, 64);
            ArgumentOutOfRangeException.ThrowIfNotEqual(IntMath.IsPowerOfTwo(alignment), true, nameof(alignment));
        }
    }
}