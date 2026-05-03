using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Memory;

public unsafe struct Pointer<T> where T : unmanaged
{
    public T* Ptr;
    
    public Pointer(T* ptr) => Ptr = ptr;
    public Pointer(NativeView<T> ptr) => Ptr = ptr;
    public Pointer(NativeView<byte> ptr) => Ptr = (T*)ptr.Ptr;

    public readonly bool IsNull => Ptr == null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(Pointer<T> p) => p.Ptr;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* operator +(Pointer<T> a, int b) => a.Ptr + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* operator -(Pointer<T> a, int b) => a.Ptr - b;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Ptr[index];
    }

}