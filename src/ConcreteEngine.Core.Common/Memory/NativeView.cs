using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Memory;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct NativeView<T> : IEquatable<NativeView<T>> where T : unmanaged
{
    public static NativeView<T> MakeNull() => new(null, 0);

    public readonly T* Ptr;
    public readonly T* EndPtr;

    public NativeView(T* ptr, int length)
    {
        Ptr = ptr;
        EndPtr = ptr + length;
    }

    internal NativeView(T* ptr, T* endPtr)
    {
        Ptr = ptr;
        EndPtr = endPtr;
    }

    public int SizeInBytes => (int)((byte*)EndPtr - (byte*)Ptr);

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)(EndPtr - Ptr);
    }


    public bool IsNull => Ptr == null;
    public bool IsEmpty => Ptr == EndPtr;

    public bool IsNullOrEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Ptr == null || Ptr == EndPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(NativeView<T> array) => array.Ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NativeView<T>(NativeArray<T> array) => new(array.Ptr, array.Length);

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
        Debug.Assert((uint)offset + (uint)length <= (uint)Length);
        return new NativeView<T>(Ptr + offset, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<T> SliceFrom(int offset)
    {
        Debug.Assert((uint)offset < (uint)Length);
        return new NativeView<T>(Ptr + offset, EndPtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan() => IsNullOrEmpty ? default : new Span<T>(Ptr, Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int offset, int length)
    {
        if (Ptr + offset + length > EndPtr) Throwers.RangeOutOfBounds(offset, length, Length);
        return IsNullOrEmpty ? default : new Span<T>(Ptr + offset, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AsReadOnlySpan() => IsNullOrEmpty ? default : new ReadOnlySpan<T>(Ptr, Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AsReadOnlySpan(int offset, int length)
    {
        if (Ptr + offset + length > EndPtr) Throwers.RangeOutOfBounds(offset, length, Length);
        return IsNullOrEmpty ? default : new ReadOnlySpan<T>(Ptr + offset, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (IsNull) Throwers.NullPointer(nameof(Ptr));
        if (IsEmpty) Throwers.InvalidOperation(nameof(IsEmpty));
        NativeMemory.Clear(Ptr, (nuint)SizeInBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<U> Reinterpret<U>() where U : unmanaged => new((U*)Ptr, (U*)EndPtr);

    public static bool operator ==(NativeView<T> left, NativeView<T> right) => left.Equals(right);
    public static bool operator !=(NativeView<T> left, NativeView<T> right) => !(left == right);

    public bool Equals(NativeView<T> other) => Ptr == other.Ptr && EndPtr == other.EndPtr;

    public override bool Equals(object? obj) => obj is NativeView<T> v && Equals(v);
    public override int GetHashCode() => HashCode.Combine((nint)Ptr, (nint)EndPtr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PtrEnumerator<T> GetEnumerator() => new(this);
    
}