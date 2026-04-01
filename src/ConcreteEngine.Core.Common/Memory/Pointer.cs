using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Memory;

public unsafe struct Pointer<T> where T : unmanaged
{
    public T* Ptr;
    public Pointer(T* ptr) => Ptr = ptr;
    public Pointer(NativeViewPtr<T> ptr) => Ptr = ptr;

    public readonly bool IsNull => Ptr == null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(Pointer<T> array) => array.Ptr;
}