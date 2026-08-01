using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Common.Memory;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct NativeView<T> : IEquatable<NativeView<T>> where T : unmanaged
{
    public static NativeView<T> MakeNull() => new(null, 0, 0);

    public readonly T* Ptr;
    public readonly int Offset;
    public readonly int Length;

    public NativeView(T* ptr, int length) : this(ptr, 0, length) { }

    public NativeView(T* ptr, int offset, int length)
    {
        Ptr = ptr;
        Offset = offset;
        Length = length;
    }

    public int End => Offset + Length;
    public bool IsNull => Ptr == null;

    public int SizeInBytes => Length * Unsafe.SizeOf<T>();
    public int OffsetInBytes => Length * Unsafe.SizeOf<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NativeView<T>(NativeArray<T> array) => new(array.Ptr, 0, array.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(NativeView<T> array) => array.Ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* operator +(NativeView<T> a, int b) => a.Ptr + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* operator -(NativeView<T> a, int b) => a.Ptr - b;

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert((uint)index < (uint)Length);
            return ref Ptr[index];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<T> Slice(int offset, int length)
    {
        Debug.Assert((uint)offset + (uint)length < (uint)Length);
        return new NativeView<T>(Ptr + offset, Offset + offset, length);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<T> SliceFrom(int offset)
    {
        Debug.Assert((uint)offset < (uint)Length);
        return new NativeView<T>(Ptr + offset, offset, Length - offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<T> Slice(RangeU16 range) => Slice(range.Offset16, range.Length16);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<T> Slice(Range32 range) => Slice(range.Offset, range.Length);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int offset = 0)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)offset, (uint)Length);
        if (IsNull) return default;
        return new Span<T>(Ptr + offset, Length - offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int offset, int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)offset + (uint)length, (uint)Length);
        if (IsNull) return default;
        return new Span<T>(Ptr + offset, length);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AsReadOnlySpan(int offset = 0)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)offset, (uint)Length);
        if (IsNull) return default;
        return new ReadOnlySpan<T>(Ptr + offset, Length - offset);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AsReadOnlySpan(int offset, int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)offset + (uint)length, (uint)Length);
        if (IsNull) return default;
        return new ReadOnlySpan<T>(Ptr + offset, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => NativeMemory.Clear(Ptr, (nuint)SizeInBytes);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<U> Reinterpret<U>() where U : unmanaged
    {
        Debug.Assert(SizeInBytes % Unsafe.SizeOf<U>() == 0);
        return new NativeView<U>((U*)Ptr, OffsetInBytes / Unsafe.SizeOf<U>(), SizeInBytes / Unsafe.SizeOf<U>());
    }

    public static bool operator ==(NativeView<T> left, NativeView<T> right) => left.Equals(right);
    public static bool operator !=(NativeView<T> left, NativeView<T> right) => !(left == right);

    public bool Equals(NativeView<T> other) => Ptr == other.Ptr && Offset == other.Offset && Length == other.Length;

    public override bool Equals(object? obj) => obj is NativeView<T> v && Equals(v);
    public override int GetHashCode() => HashCode.Combine((IntPtr)Ptr, Offset, Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PtrEnumerator<T> GetEnumerator() => new(Ptr, Length);
    
}
