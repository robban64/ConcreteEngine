using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Gfx.Resources.Handles;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Renderer.Data;

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawCommand(
    MeshId meshId,
    MaterialId materialId,
    int instanceCount = 0,
    ushort animationSlot = 0,
    DrawCommandResolver resolver = DrawCommandResolver.None)
{
    public readonly MeshId MeshId = meshId;
    public readonly int InstanceCount = instanceCount;
    public readonly MaterialId MaterialId = materialId;
    public readonly ushort AnimationSlot = animationSlot;
    public readonly DrawCommandResolver Resolver = resolver;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawCommandMeta(
    DrawCommandId id,
    DrawCommandQueue queue,
    PassMask passMask = PassMask.Default,
    ushort depthKey = 0)
{
    public readonly ushort DepthKey = depthKey;
    public readonly PassMask PassMask = passMask;
    public readonly DrawCommandId Id = id;
    public readonly DrawCommandQueue Queue = queue;
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