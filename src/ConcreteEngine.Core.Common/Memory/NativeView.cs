using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;

namespace ConcreteEngine.Core.Common.Memory;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct NativeView<T>(T* ptr, int offset, int length)
    : IEquatable<NativeView<T>> where T : unmanaged
{
    public readonly T* Ptr = ptr;
    public readonly int Offset = offset;
    public readonly int Length = length;

    public NativeView(T* ptr, int length) : this(ptr, 0, length) { }

    public int End => Offset + Length;
    public bool IsNull => Ptr == null;
    public int SizeInBytes => Length * Unsafe.SizeOf<T>();

    public static bool operator ==(NativeView<T> left, NativeView<T> right) => left.Equals(right);
    public static bool operator !=(NativeView<T> left, NativeView<T> right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NativeView<T>(NativeArray<T> array) => new(array.Ptr, array.Length);

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
        Debug.Assert((uint)offset + (uint)length <= (uint)Length);
        return new NativeView<T>(Ptr + offset, Offset + offset, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<T> SliceFrom(int offset)
    {
        Debug.Assert((uint)offset <= (uint)Length);
        return new NativeView<T>(Ptr + offset, offset, Length - offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int offset = 0)
    {
        Debug.Assert((uint)offset <= (uint)Length);
        return new Span<T>(Ptr + offset, Length - offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int offset, int length)
    {
        Debug.Assert((uint)offset + (uint)length <= (uint)Length);
        return new Span<T>(Ptr + offset, length);
    }


    public void Clear() => NativeMemory.Clear(Ptr, (nuint)SizeInBytes);

    public NativeView<U> Reinterpret<U>() where U : unmanaged
    {
        Debug.Assert(SizeInBytes % Unsafe.SizeOf<U>() == 0);
        return new NativeView<U>((U*)Ptr, Offset, SizeInBytes / Unsafe.SizeOf<U>());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(NativeView<T> other) => Ptr == other.Ptr && Offset == other.Offset && Length == other.Length;

    public override bool Equals(object? obj) => obj is NativeView<T> v && Equals(v);
    public override int GetHashCode() => HashCode.Combine((IntPtr)Ptr, Offset, Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefEnumerator<T> GetEnumerator() => new(ref *Ptr, Length);
}