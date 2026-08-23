using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public readonly record struct RenderEntityId(int Id) : IComparable<RenderEntityId>
{
    public readonly int Id = Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Id > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(RenderEntityId other) => Id.CompareTo(other.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator int(RenderEntityId e) => e.Id;
}

[StructLayout(LayoutKind.Explicit)]
public readonly struct DrawEntityIndex : IComparable<DrawEntityIndex>
{
    [FieldOffset(0)] private readonly ulong _sortKey;

    //
    [FieldOffset(0)] public readonly PassMask Mask;
    [FieldOffset(1)] public readonly RenderEntityId Entity;
    [FieldOffset(5)] private readonly ushort _depthKey;
    [FieldOffset(7)] private readonly DrawQueue _queue;

    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawEntityIndex(RenderEntityId entity, PassMask mask, DrawQueue queue, ushort depthKey)
    {
        Entity = entity;
        Mask = mask;
        _queue = queue;
        _depthKey = depthKey;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RenderEntityId(DrawEntityIndex e) => e.Entity;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Entity.Id - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Entity.Id > 0;

    public int CompareTo(DrawEntityIndex other) => _sortKey.CompareTo(other._sortKey);
}