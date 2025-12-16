using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Worlds.Entities.Components;

[StructLayout(LayoutKind.Sequential)]
public struct SourceComponent(
    ModelId model,
    int drawCount,
    MaterialTagKey materialTagKey,
    EntitySourceKind kind) : IEntityComponent
{
    public int DrawCount = drawCount;
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialTagKey;
    public EntitySourceKind Kind = kind;
}

[StructLayout(LayoutKind.Sequential)]
public struct ParticleComponent(int emitterHandle, MaterialId material) : IEntityComponent
{
    public int EmitterHandle = emitterHandle;
    public MaterialId Material = material;

    public static BoundingBox DefaultParticleBounds => new(new Vector3(-0.5f), new Vector3(0.5f));
}