using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Common.Memory;

public unsafe struct NativeView<T>(T* ptr, int offset, int length) : IEquatable<NativeView<T>> where T : unmanaged
{
    public T* Ptr = ptr;
    public readonly int Offset = offset;
    public readonly int Length = length;

    public NativeView(T* ptr, int length) : this(ptr, 0, length) { }

    public readonly int End => Offset + Length;
    public readonly bool IsNull => Ptr == null;
    public readonly int SizeInBytes => Length * Unsafe.SizeOf<T>();


    public static bool operator ==(NativeView<T> left, NativeView<T> right) => left.Equals(right);

    public static bool operator !=(NativeView<T> left, NativeView<T> right) => !(left == right);

    public static implicit operator NativeView<T>(NativeArray<T> array) => new(array.Ptr, array.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(NativeView<T> array) => array.Ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* operator +(NativeView<T> a, int b) => a.Ptr + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* operator -(NativeView<T> a, int b) => a.Ptr - b;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Ptr[index];
    }

    public readonly void Clear() => NativeMemory.Clear(Ptr, (nuint)SizeInBytes);

    public readonly NativeView<U> Reinterpret<U>() where U : unmanaged
    {
        Debug.Assert(SizeInBytes % Unsafe.SizeOf<U>() == 0);
        return new NativeView<U>((U*)Ptr, Offset, SizeInBytes / Unsafe.SizeOf<U>());
    }

    public readonly bool Equals(NativeView<T> other) => Ptr == other.Ptr && Offset == other.Offset && Length == other.Length;
    public override readonly bool Equals(object? obj) => obj is NativeView<T> v && Equals(v);
    public override readonly int GetHashCode() => HashCode.Combine((IntPtr)Ptr, Offset, Length);

    public readonly ValuePtrEnumerator<T> GetEnumerator() => new(ref *Ptr, Length);

}