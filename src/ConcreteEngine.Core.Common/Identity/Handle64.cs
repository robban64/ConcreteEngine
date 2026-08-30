using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Identity;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Handle64<T>(int Id, int Gen)
    : ITypedHandle<Handle64<T>>, IComparable<int>, IComparable<Handle64<T>>
{
    public static readonly Handle64<T> Empty = default;

    public int Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id - 1;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id > 0 && Gen > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(Handle64<T> id) => id.Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Handle64<T> a, int b) => a.Id > b && a.Id > b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Handle64<T> a, int b) => a.Id < b && a.Id < b;

    public int CompareTo(int other) => Id.CompareTo(other);
    public int CompareTo(Handle64<T> other) => Id.CompareTo(other.Id);
    
    public static ulong Pack(Handle64<T> handle) => Unsafe.BitCast<Handle64<T>, ulong>(handle);
    public static Handle64<T> Unpack(ulong packedHandle) => Unsafe.BitCast<ulong, Handle64<T>>(packedHandle);
}