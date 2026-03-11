using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory.Enumerators;
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
        get
        {
            Debug.Assert((uint)index < (uint)Length, $"Index {index} out of range [0, {Length})");
            return ref Ptr[index];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeViewPtr<T> Slice(int offset, int length)
    {
        if ((uint)offset + (uint)length > (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        return new NativeViewPtr<T>(Ptr + offset, offset, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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


    public readonly void CopyTo(NativeArray<T> dest, int srcOffset = 0, int dstOffset = 0, int count = -1)
    {
        if (count < 0) count = Length - srcOffset;

        if ((uint)srcOffset + (uint)count > (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(srcOffset));

        if ((uint)dstOffset + (uint)count > (uint)dest.Length)
            throw new ArgumentOutOfRangeException(nameof(dstOffset));

        Unsafe.CopyBlockUnaligned(dest + dstOffset, Ptr + srcOffset, (uint)(count * Unsafe.SizeOf<T>()));
    }

    public readonly void Clear() => NativeMemory.Clear(Ptr, (nuint)SizeInBytes);


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
#if DEBUG
        Console.WriteLine($"Reallocate {nameof(NativeArray)}: {bytes} bytes");
#endif
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Dispose()
    {
        if (Ptr == null) return;
        var capacity = SizeInBytes;

        if (Alignment > 0)
        {
            NativeMemory.AlignedFree(Ptr);
        }
        else
        {
            NativeMemory.Free(Ptr);
        }

        Ptr = null;
        Length = 0;
#if DEBUG
        Console.WriteLine($"Disposed {nameof(NativeArray)}: {capacity} bytes");
#endif
    }

    public readonly PtrEnumerator<T> GetEnumerator() => new(Ptr, Length);
}

public unsafe struct NativeViewPtr<T>(T* ptr, int offset, int length) where T : unmanaged
{
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

    public readonly NativeViewPtr<U> Reinterpret<U>() where U : unmanaged
    {
        Debug.Assert(SizeInBytes % Unsafe.SizeOf<U>() == 0);
        return new NativeViewPtr<U>((U*)Ptr, Offset, SizeInBytes / Unsafe.SizeOf<U>());
    }

    public readonly PtrEnumerator<T> GetEnumerator() => new(Ptr, Length);
}
