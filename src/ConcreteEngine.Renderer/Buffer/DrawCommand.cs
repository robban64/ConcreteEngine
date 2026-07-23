using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Renderer.Buffer;

[StructLayout(LayoutKind.Sequential)]
public struct DrawCommand(
    MeshId meshId,
    Id16<MaterialSlot> materialId,
    uint instanceCount = 0,
    ushort animationSlot = 0,
    PassMask passes = PassMask.Default,
    DrawCommandResolver resolver = DrawCommandResolver.None,
    byte resolverSlot = 0)
{
    public uint InstanceCount = instanceCount;
    public MeshId MeshId = meshId;
    public Id16<MaterialSlot> MaterialId = materialId;
    public ushort AnimationSlot = animationSlot;
    public DrawCommandResolver Resolver = resolver;
    public byte ResolverSlot = resolverSlot;
}


[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawCommandIndex : IComparable<DrawCommandIndex>
{
    private readonly ulong _sortKey;
    public readonly int Index; // submit index, stable sort
    public readonly PassMask Pass;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawCommandIndex(int index, PassMask pass, DrawCommandQueue queue, ushort depthKey)
    {
        Index = index;
        Pass = pass;

        var depth = queue < DrawCommandQueue.Transparent ? depthKey : (ushort)(ushort.MaxValue - depthKey);
        _sortKey = ((ulong)queue << 48) |
                   ((ulong)depth << 32) |
                   (uint)index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(DrawCommandIndex other) => _sortKey.CompareTo(other._sortKey);
}