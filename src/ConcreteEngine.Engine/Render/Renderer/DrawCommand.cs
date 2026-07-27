using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Engine.Render.Renderer;

[StructLayout(LayoutKind.Sequential)]
public struct DrawCommand(
    MeshId meshId,
    Id16<Material> materialId,
    uint instanceCount = 0,
    ushort animationSlot = 0,
    DrawCommandResolver resolver = DrawCommandResolver.None,
    byte resolverSlot = 0)
{
    public uint InstanceCount = instanceCount;
    public MeshId MeshId = meshId;
    public Id16<Material> MaterialId = materialId;
    public ushort AnimationSlot = animationSlot;
    public DrawCommandResolver Resolver = resolver;
    public byte ResolverSlot = resolverSlot;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawCommandIndex : IComparable<DrawCommandIndex>
{
    private readonly ulong _sortKey;
    
    // submit index, stable sort
    public int Index => (int)((_sortKey >> 8) & 0x00FF_FFFF);
    public PassMask Pass => (PassMask)_sortKey;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawCommandIndex(int index, PassMask pass, DrawQueue queue, ushort depthKey)
    {
        var depth = queue < DrawQueue.Transparent ? depthKey : (ushort)(ushort.MaxValue - depthKey);
        _sortKey = ((ulong)queue << 48) |
                   ((ulong)depth << 32) |
                   ((ulong)index << 8) |
                   (byte)pass;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(DrawCommandIndex other) => _sortKey.CompareTo(other._sortKey);
}