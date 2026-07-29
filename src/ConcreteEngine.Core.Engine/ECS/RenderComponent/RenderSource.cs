using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

public struct DrawPolicy(DrawQueue queue, PassMask passes)
{
    public DrawQueue Queue = queue;
    public PassMask Passes = passes;
}

public struct RenderSource(
    MeshId mesh,
    Id16<Material> material,
    int meshIndex,
    EntitySourceKind kind)
{
    public uint InstanceCount;
    public MeshId Mesh = mesh;
    public Id16<Material> Material = material;

    public byte MeshIndex = (byte)meshIndex;
    public EntitySourceKind Kind = kind;
}