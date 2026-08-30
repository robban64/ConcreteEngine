using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Identity;

namespace ConcreteEngine.Core.Engine.ECS.Render;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct RenderEntity : ITypedHandle<RenderEntity>, IComparable<RenderEntity>
{
    public readonly int Entity;
    public readonly int Gen;

    public RenderEntity(int entity, int gen)
    {
        Entity = entity;
        Gen = gen;
    }

    public int Index => Entity;
    public int Generation => Gen;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Entity >= 0 && Gen > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator int(RenderEntity e) => e.Entity;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(RenderEntity other) => Entity.CompareTo(other.Entity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Pack(RenderEntity e) => Unsafe.BitCast<RenderEntity, ulong>(e);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RenderEntity Unpack(ulong packed) => Unsafe.BitCast<ulong, RenderEntity>(packed);
}

public readonly record struct RenderEntityId(int Id)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(RenderEntityId e) => e.Id;

}