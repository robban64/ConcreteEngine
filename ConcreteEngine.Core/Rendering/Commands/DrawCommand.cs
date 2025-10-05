#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Commands;

public readonly struct DrawCommand(MeshId meshId, MaterialId materialId, int drawCount = 0)
{
    public readonly MeshId MeshId = meshId;
    public readonly MaterialId MaterialId = materialId;
    public readonly int DrawCount = drawCount;
}

public readonly struct DrawTransformPayload(in Matrix4x4 transform)
{
    public readonly Matrix4x4 Transform = transform;
}

public readonly struct DrawCommandMeta(
    DrawCommandId id,
    DrawCommandQueue queue,
    PassMask passMask = PassMask.Default,
    ushort depthKey = 0)
{
    public readonly PassMask PassMask = passMask;
    public readonly ushort DepthKey = depthKey;
    public readonly DrawCommandId Id = id;
    public readonly DrawCommandQueue Queue = queue;
}

internal readonly struct DrawCommandRef : IComparable<DrawCommandRef>
{
    private readonly ulong _sortKey;
    public readonly int Idx; // submit index, stable sort

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawCommandRef(DrawCommandMeta meta, int idx)
    {
        Idx = idx;
        _sortKey = ((ulong)meta.Queue << 48) |
                   ((ulong)meta.DepthKey << 32) |
                   (uint)idx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(DrawCommandRef other) => _sortKey.CompareTo(other._sortKey);
}

internal readonly record struct DrawCommandTicket(int SubmitIdx, byte PassId);

internal readonly record struct DrawPassRange(int Start, int Count);