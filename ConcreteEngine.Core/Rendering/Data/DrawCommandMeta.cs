using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Rendering;

public readonly struct DrawCommandMeta(
    DrawCommandId id,
    DrawCommandTag tag,
    RenderTargetId target,
    DrawCommandQueue queue,
    byte layer = 0,
    byte view = 0,
    ushort depthKey = 0)
{
    public readonly DrawCommandId Id = id;
    public readonly DrawCommandTag Tag = tag;
    public readonly RenderTargetId Target = target;
    public readonly DrawCommandQueue Queue = queue;
    public readonly byte Layer = layer;
    public readonly byte View = view;
    public readonly ushort DepthKey = depthKey;

    public static DrawCommandMeta Make2D(DrawCommandId id, DrawCommandTag tag, RenderTargetId target, byte layer = 0)
        => new (id, tag, target, DrawCommandQueue.None, layer: layer);
}

internal readonly struct DrawCommandMetaIndex(in DrawCommandMeta meta, int idx)
    : IComparable<DrawCommandMetaIndex>
{
    public readonly DrawCommandMeta Meta = meta;
    public readonly int Idx = idx;

    private readonly ulong _sortKey =
        ((ulong)(byte)meta.Target << 56) |
        ((ulong)meta.View << 48) |
        ((ulong)meta.Queue << 40) |
        ((ulong)meta.DepthKey << 24) |
        ((ulong)meta.Layer << 16) |
        (ushort)idx;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(DrawCommandMetaIndex other) => _sortKey.CompareTo(other._sortKey);
}