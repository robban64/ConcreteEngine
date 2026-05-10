using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Renderer.Data;

[StructLayout(LayoutKind.Sequential)]
public struct DrawCommand(
    MeshId meshId,
    MaterialId materialId,
    uint instanceCount = 0,
    ushort animationSlot = 0)
{
    public MeshId MeshId = meshId;
    public uint InstanceCount = instanceCount;
    public MaterialId MaterialId = materialId;
    public ushort AnimationSlot = animationSlot;
}

[StructLayout(LayoutKind.Sequential)]
public struct DrawCommandMeta(
    DrawCommandId id,
    DrawCommandQueue queue,
    PassMask passMask = PassMask.Default,
    ushort depthKey = 0,
    DrawCommandResolver resolver = DrawCommandResolver.None,
    byte resolverSlot = 0)
{
    public ushort DepthKey = depthKey;
    public PassMask PassMask = passMask;
    public DrawCommandId Id = id;
    public DrawCommandQueue Queue = queue;
    public DrawCommandResolver Resolver = resolver;
    public byte ResolverSlot = resolverSlot;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct DrawCommandRef : IComparable<DrawCommandRef>
{
    private readonly ulong _sortKey;
    public readonly int Index; // submit index, stable sort

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawCommandRef(DrawCommandMeta meta, int index)
    {
        Index = index;
        _sortKey = ((ulong)meta.Queue << 48) |
                   ((ulong)meta.DepthKey << 32) |
                   (uint)index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(DrawCommandRef other) => _sortKey.CompareTo(other._sortKey);
}