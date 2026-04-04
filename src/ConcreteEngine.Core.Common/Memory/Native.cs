using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;

public static class NativeArray
{
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
        else
        {
            return zeroed
                ? NativeMemory.AllocZeroed((nuint)length, (nuint)stride)
                : NativeMemory.Alloc((nuint)length, (nuint)stride);
        }
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
        Console.WriteLine($"Reallocate {nameof(NativeArray)}: {bytes} bytes");
#endif
        return ptr;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static unsafe void DisposeArray(void* ptr, int capacity, int alignment)
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
        get
        {
            Debug.Assert((uint)index < (uint)Length, $"Index {index} out of range [0, {Length})");
            return ref Ptr[index];
        }
    }

    public readonly NativeViewPtr<T> Slice(int offset, int length = 0)
    {
        if ((uint)offset + (uint)length > (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        return new NativeViewPtr<T>(Ptr + offset, offset, length > 0 ? length : Length - offset);
    }

    public readonly Span<T> AsSpan(int offset = 0)
    {
        if ((uint)offset > (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        return new Span<T>(Ptr + offset, Length - offset);
    }

    public readonly Span<T> AsSpan(int offset, int length)
    {
        if ((uint)offset + (uint)length > (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        return new Span<T>(Ptr + offset, length);
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

public unsafe struct NativeViewPtr<T>(T* ptr, int offset, int length) where T : unmanaged
{
    public static NativeViewPtr<T> MakeNull() => new(null, 0, 0);

    public T* Ptr = ptr;
    public readonly int Offset = offset;
    public readonly int Length = length;

    public readonly bool IsNull => Ptr == null;
    public readonly int SizeInBytes => Length * Unsafe.SizeOf<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(NativeViewPtr<T> array) => array.Ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* operator +(NativeViewPtr<T> a, int b) => a.Ptr + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* operator -(NativeViewPtr<T> a, int b) => a.Ptr - b;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert((uint)index < (uint)Length, $"Index {index} out of range [0, {Length})");
            return ref Ptr[index];
        }
    }

    public readonly NativeViewPtr<T> Slice(int offset, int length)
    {
        if ((uint)offset + (uint)length > (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        return new NativeViewPtr<T>(Ptr + offset, Offset + offset, length);
    }

    public readonly NativeViewPtr<T> SliceFrom(int offset)
    {
        if ((uint)offset > (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        return new NativeViewPtr<T>(Ptr + offset, offset, Length - offset);
    }

    public readonly Span<T> AsSpan(int offset = 0)
    {
        if ((uint)offset > (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        return new Span<T>(Ptr + offset, Length - offset);
    }

    public readonly Span<T> AsSpan(int offset, int length)
    {
        if ((uint)offset + (uint)length > (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        return new Span<T>(Ptr + offset, length);
    }

    public readonly void CopyTo(NativeViewPtr<T> dest, int srcOffset = 0, int dstOffset = 0, int count = -1)
    {
        if (count < 0) count = Length - srcOffset;

        if ((uint)srcOffset + (uint)count > (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(srcOffset));

        if ((uint)dstOffset + (uint)count > (uint)dest.Length)
            throw new ArgumentOutOfRangeException(nameof(dstOffset));

        Unsafe.CopyBlockUnaligned(dest + dstOffset, Ptr + srcOffset, (uint)(count * Unsafe.SizeOf<T>()));
    }

    public readonly void Clear() => NativeMemory.Clear(Ptr, (nuint)SizeInBytes);

    public readonly NativeViewPtr<U> Reinterpret<U>() where U : unmanaged
    {
        Debug.Assert(SizeInBytes % Unsafe.SizeOf<U>() == 0);
        return new NativeViewPtr<U>((U*)Ptr, Offset, SizeInBytes / Unsafe.SizeOf<U>());
    }

    public readonly ValuePtrEnumerator<T> GetEnumerator() => new(ref *Ptr, Length);
}