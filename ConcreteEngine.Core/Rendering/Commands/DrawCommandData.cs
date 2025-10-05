#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Core.Rendering.Passes;

#endregion

namespace ConcreteEngine.Core.Rendering.Commands;

internal readonly struct DrawCommandMetaIndex : IComparable<DrawCommandMetaIndex>
{
    private readonly ulong _sortKey;
    public readonly int Idx; // submit index, stable sort

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawCommandMetaIndex(DrawCommandMeta meta, int idx)
    {
        Idx = idx;
        _sortKey = ((ulong)meta.Queue << 48) |
                   ((ulong)meta.DepthKey << 32) |
                   (uint)idx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(DrawCommandMetaIndex other) => _sortKey.CompareTo(other._sortKey);
}

internal readonly record struct DrawTicket(int SubmitIdx, byte PassId);

internal readonly record struct PassRange(int Start, int Count);
