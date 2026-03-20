using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Renderer.Data;

[StructLayout(LayoutKind.Sequential)]
public  struct DrawCommand(
    MeshId meshId,
    MaterialId materialId,
    int instanceCount = 0,
    ushort animationSlot = 0,
    DrawCommandResolver resolver = DrawCommandResolver.None)
{
    public  MeshId MeshId = meshId;
    public  int InstanceCount = instanceCount;
    public  MaterialId MaterialId = materialId;
    public  ushort AnimationSlot = animationSlot;
    public  DrawCommandResolver Resolver = resolver;
}

[StructLayout(LayoutKind.Sequential)]
public  struct DrawCommandMeta(
    DrawCommandId id,
    DrawCommandQueue queue,
    PassMask passMask = PassMask.Default,
    ushort depthKey = 0)
{
    public  ushort DepthKey = depthKey;
    public  PassMask PassMask = passMask;
    public  DrawCommandId Id = id;
    public  DrawCommandQueue Queue = queue;
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