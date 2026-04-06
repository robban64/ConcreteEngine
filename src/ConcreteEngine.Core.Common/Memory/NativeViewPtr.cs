using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Common.Memory;

public unsafe struct NativeViewPtr<T>(T* ptr, int offset, int length) where T : unmanaged
{
    public T* Ptr = ptr;
    public readonly int Offset = offset;
    public readonly int Length = length;

    public NativeViewPtr(T* ptr, int length) : this(ptr, 0, length) { }

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

    public readonly void Clear() => NativeMemory.Clear(Ptr, (nuint)SizeInBytes);

    public readonly NativeViewPtr<U> Reinterpret<U>() where U : unmanaged
    {
        Debug.Assert(SizeInBytes % Unsafe.SizeOf<U>() == 0);
        return new NativeViewPtr<U>((U*)Ptr, Offset, SizeInBytes / Unsafe.SizeOf<U>());
    }

    public readonly ValuePtrEnumerator<T> GetEnumerator() => new(ref *Ptr, Length);
}