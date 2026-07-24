using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

public struct DrawPolicy(DrawQueue queue, PassMask passes)
{
    public DrawQueue Queue = queue;
    public PassMask Passes = passes;
}

public struct RenderSource(
    MeshId mesh,
    Id16<MaterialSlot> material,
    int meshIndex,
    EntitySourceKind kind)
{
    public MeshId Mesh = mesh;
    public Id16<MaterialSlot> Material = material;
    
    public byte MeshIndex = (byte)meshIndex;
    public EntitySourceKind Kind = kind;
    public ushort AnimationSlot = 0;

    // maybe rework this
    public DrawCommandResolver Resolver;
    public byte ResolverSlot;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly void WriteTo(scoped ref DrawCommand cmd)
    {
        cmd.MeshId = Mesh;
        cmd.MaterialId = Material;
        cmd.AnimationSlot = AnimationSlot;
        cmd.Resolver = Resolver;
        cmd.ResolverSlot = ResolverSlot;
    }

    internal void SetResolve(DrawCommandResolver resolver, byte resolverSlot)
    {
        Resolver = resolver;
        ResolverSlot = resolverSlot;
    }
}