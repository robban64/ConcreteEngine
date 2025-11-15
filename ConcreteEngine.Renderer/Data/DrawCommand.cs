#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Renderer.Data;

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawCommand(MeshId meshId, MaterialId materialId, int drawCount = 0)
{
    public readonly MeshId MeshId = meshId;
    public readonly MaterialId MaterialId = materialId;
    public readonly int DrawCount = drawCount;
    private readonly int pad;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawCommandMeta(
    DrawCommandId id,
    DrawCommandQueue queue,
    DrawCommandResolver resolver = DrawCommandResolver.None,
    PassMask passMask = PassMask.Default,
    ushort depthKey = 0)
{
    public readonly PassMask PassMask = passMask;
    public readonly ushort DepthKey = depthKey;
    public readonly DrawCommandId Id = id;
    public readonly DrawCommandQueue Queue = queue;
    public readonly DrawCommandResolver Resolver = resolver;
}

[StructLayout(LayoutKind.Sequential)]
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

internal readonly struct DrawCommandTicket(int submitIdx, byte passId, DrawCommandResolver resolver)
{
    public readonly int SubmitIdx  = submitIdx;
    public readonly byte PassId  = passId;
    public readonly DrawCommandResolver Resolver  = resolver;
}

internal readonly struct DrawPassRange(int start, int count)
{
    public readonly int Start = start;
    public readonly int Count = count;
}