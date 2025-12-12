#region

using System.Runtime.InteropServices;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Worlds.Entities.Components;

public enum RenderSourceKind : byte
{
    Model,
    Particle,
}

public enum RenderResolver : byte
{
    None = 0,
    Wireframe = 1,
    Highlight = 2,
    BoundingVolume = 2,
}

[StructLayout(LayoutKind.Sequential)]
public struct RenderSourceComponent(
    ModelId model,
    int drawCount,
    MaterialTagKey materialTagKey,
    RenderSourceKind kind = RenderSourceKind.Model,
    RenderResolver resolver = RenderResolver.None)
{
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialTagKey;
    public int DrawCount = drawCount;
    public RenderSourceKind Kind = kind;
    public RenderResolver Resolver = resolver;
}

[StructLayout(LayoutKind.Sequential)]
public struct ParticleComponent(int emitterHandle, MaterialId material)
{
    public int EmitterHandle = emitterHandle;
    public MaterialId Material = material;
}