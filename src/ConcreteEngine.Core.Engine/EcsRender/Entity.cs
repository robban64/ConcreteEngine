using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Identity;

namespace ConcreteEngine.Core.Engine.EcsRender;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct RenderEntity(int Id, ushort Gen) : ITypedHandle<RenderEntity>, IComparable<RenderEntity>
{
    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id > 0 && Gen > 0;
    }

    public int Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator int(RenderEntity e) => e.Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(RenderEntity other) => Id.CompareTo(other.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Pack(RenderEntity e) => Unsafe.BitCast<RenderEntity, ulong>(e);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RenderEntity Unpack(ulong packed) => Unsafe.BitCast<ulong, RenderEntity>(packed);
}

public readonly record struct RenderEntityIndex(int Id)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator RenderEntity(RenderEntityIndex e) => new(e.Id, ushort.MaxValue);
    public int Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id - 1;
    }

}