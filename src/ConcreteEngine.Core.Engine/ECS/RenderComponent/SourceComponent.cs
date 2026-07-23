using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

public struct SortComponent(DrawCommandQueue queue, PassMask passes)
{
    public DrawCommandQueue Queue = queue;
    public PassMask Passes = passes;
}

[StructLayout(LayoutKind.Sequential)]
public struct SourceComponent(
    MeshId mesh,
    Id16<MaterialSlot> material,
    int meshIndex,
    EntitySourceKind kind,
    DrawCommandQueue queue,
    PassMask passes)
{
    public MeshId Mesh = mesh;
    public Id16<MaterialSlot> Material = material;

    public byte MeshIndex = (byte)meshIndex;
    public PassMask Passes = passes;
    public DrawCommandQueue Queue = queue;

    public EntitySourceKind Kind = kind;

    public ushort AnimationSlot = 0;

    // maybe rework this
    public DrawCommandResolver Resolver;
    public byte ResolverSlot;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly void WriteCommand(scoped ref DrawCommand cmd)
    {
        cmd.MeshId = Mesh;
        cmd.MaterialId = Material;
        cmd.AnimationSlot = AnimationSlot;
        cmd.Resolver = Resolver;
        cmd.ResolverSlot = ResolverSlot;
    }
}