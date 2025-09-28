using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Resources;

namespace ConcreteEngine.Core.Rendering;


public readonly struct DrawCommandMeta(
    DrawCommandId id,
    RenderTargetId target,
    DrawCommandQueue queue,
    byte order = 0,
    ushort depthKey = 0)
{
    public readonly ushort DepthKey = depthKey;
    public readonly DrawCommandId Id = id;
    public readonly RenderTargetId Target = target;
    public readonly DrawCommandQueue Queue = queue;
    public readonly byte Order = order;

    public static DrawCommandMeta Make2D(DrawCommandId id, RenderTargetId target, byte layer = 0) =>
        new(id, target, DrawCommandQueue.None, order: layer);
}

internal readonly struct DrawCommandMetaIndex(in DrawCommandMeta meta, int idx)
    : IComparable<DrawCommandMetaIndex>
{
    private readonly ulong _sortKey =
        ((ulong)(byte)meta.Target << 56) |
        ((ulong)meta.Queue << 48) |
        ((ulong)meta.DepthKey << 32) |
        ((ulong)meta.Order << 24) |
        (ushort)idx;

    public readonly int Idx = idx; // submit index, stable sort

    public readonly DrawCommandMeta Meta = meta;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(DrawCommandMetaIndex other) => _sortKey.CompareTo(other._sortKey);
}

internal static class MetaOrders
{
    public static byte OpaqueOrder(MaterialId materialId) => (byte)materialId.Id;
}