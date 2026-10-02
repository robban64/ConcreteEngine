using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.Render;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct RenderEntity : IComparable<RenderEntity>
{
    public readonly int Id;
    public readonly int Gen;

    public RenderEntity(int id, int gen)
    {
        Id = id;
        Gen = gen;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id >= 0 && Gen > 0;
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

public readonly record struct RenderEntityId(int Id)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(RenderEntityId e) => e.Id;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawEntityKey(int entity, uint sortKey)
{
    public readonly int Entity = entity;
    public readonly uint SortKey = sortKey;

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DrawEntityKey Create(int entity, PassMask passMask, float distance, DrawQueue queue)
    {
        uint clampedDistance = (uint)float.Clamp(distance, 0f, 65535f);
        uint depthKey = queue < DrawQueue.Transparent ? clampedDistance : clampedDistance ^ ushort.MaxValue;
        var sortKey = (byte)passMask | (depthKey << 8) | ((uint)queue << 24);
        return new DrawEntityKey(entity, sortKey);
    }
}