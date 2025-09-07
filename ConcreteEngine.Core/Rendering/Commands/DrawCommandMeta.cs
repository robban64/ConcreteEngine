using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;


public readonly struct DrawCommandMeta(
    DrawCommandId id,   // Id of the type
    RenderTargetId target, // when to draw
    DrawCommandQueue queue, // priority
    byte order = 0, // used for 2D mostly, ZIndex,
    ushort depthKey = 0)
{
    public readonly DrawCommandId Id = id;
    public readonly RenderTargetId Target = target;
    public readonly DrawCommandQueue Queue = queue;
    public readonly byte Order = order;
    public readonly ushort DepthKey = depthKey;

    public static DrawCommandMeta Make2D(DrawCommandId id,  RenderTargetId target, byte layer = 0)
        => new(id,  target, DrawCommandQueue.None, order: layer);
}

// used for sorting all commands and getting them by idx size stride
internal readonly struct DrawCommandMetaIndex(in DrawCommandMeta meta, int idx)
    : IComparable<DrawCommandMetaIndex>
{
    public readonly DrawCommandMeta Meta = meta;
    public readonly int Idx = idx; // submit index, stable sort

    private readonly ulong _sortKey =
        ((ulong)(byte)meta.Target << 56) |
        ((ulong)meta.Queue << 48) |
        ((ulong)meta.DepthKey << 32) |
        ((ulong)meta.Order << 24) |
        (ushort)idx;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(DrawCommandMetaIndex other) => _sortKey.CompareTo(other._sortKey);
}

internal static class MetaOrders
{
    public static byte OpaqueOrder(MaterialId materialId) => (byte)materialId.Id;
}