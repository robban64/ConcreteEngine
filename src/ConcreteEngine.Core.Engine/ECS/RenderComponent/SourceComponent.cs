using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct SourceComponent(
    MeshId mesh,
    MaterialId material,
    int meshIndex,
    EntitySourceKind kind,
    DrawCommandQueue queue,
    PassMask passes)
    : IRenderComponent<SourceComponent>
{
    public MeshId Mesh = mesh;
    public MaterialId Material = material;

    public PassMask Passes = passes;
    public byte MeshIndex = (byte)meshIndex;
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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly void WriteMeta(scoped ref DrawCommandMeta meta, ushort depth)
    {
        meta.Id = DrawCommandId.Model;
        meta.Queue = Queue;
        meta.Passes = Passes;
        meta.DepthKey = Queue < DrawCommandQueue.Transparent ? depth : (ushort)(ushort.MaxValue - depth);
        meta.Resolver = Resolver;
        meta.ResolverSlot = ResolverSlot;
    }
}