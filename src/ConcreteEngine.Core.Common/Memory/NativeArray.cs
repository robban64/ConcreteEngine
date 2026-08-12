using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;


[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeArray<T> : IDisposable where T : unmanaged
{
    public static NativeArray<T> MakeNull() => new(null, 0, 0);

    public T* Ptr;
    public int Length;
    public readonly int Alignment;

    internal NativeArray(T* ptr, int length, int alignment)
    {
        Ptr = ptr;
        Length = length;
        Alignment = alignment;
    }

    public readonly bool IsNull => Ptr == null;
    public readonly int SizeInBytes => Length * Unsafe.SizeOf<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(NativeArray<T> array) => array.Ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* operator +(NativeArray<T> a, nint b) => a.Ptr + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* operator -(NativeArray<T> a, nint b) => a.Ptr - b;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert((uint)index < (uint)Length);
            return ref Ptr[index];
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<T> AsView() => new(Ptr, 0, Length);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<T> Slice(int offset, int length = 0)
    {
        Debug.Assert((uint)offset + (uint)length <= (uint)Length);
        return new NativeView<T>(Ptr + offset, offset, length > 0 ? length : Length - offset);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan(int offset = 0)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)offset, (uint)Length);
        if (IsNull) return default;
        return new Span<T>(Ptr + offset, Length - offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan(int offset, int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)offset + (uint)length, (uint)Length);
        if (IsNull) return default;
        return new Span<T>(Ptr + offset, length);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<T> AsReadOnlySpan(int offset = 0)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)offset, (uint)Length);
        if (IsNull) return default;
        return new ReadOnlySpan<T>(Ptr + offset, Length - offset);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<T> AsReadOnlySpan(int offset, int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)offset + (uint)length, (uint)Length);
        if (IsNull) return default;
        return new ReadOnlySpan<T>(Ptr + offset, length);
    }
    
    public readonly void Clear() => NativeMemory.Clear(Ptr, (nuint)SizeInBytes);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ReAlloc(int newLength, bool zeroed)
    {
        Ptr = (T*)NativeArray.ReAlloc(Ptr, Length, newLength, Unsafe.SizeOf<T>(), Alignment, zeroed);
        Length = newLength;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Dispose()
    {
        if (Ptr != null) NativeArray.DisposeArray(Ptr, SizeInBytes, Alignment);
        Ptr = null;
        Length = 0;
    }
}