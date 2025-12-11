#region

using System.Runtime.InteropServices;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Engine.Worlds.Entities.Components;

public enum RenderSourceKind : byte
{
    Model,
    Particle,
}

[StructLayout(LayoutKind.Sequential)]
public struct RenderSourceComponent(
    ModelId model,
    int drawCount,
    MaterialTagKey materialTagKey,
    RenderSourceKind kind = RenderSourceKind.Model)
{
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialTagKey;
    public int DrawCount = drawCount;
    public RenderSourceKind Kind = kind;
}

[StructLayout(LayoutKind.Sequential)]
public struct ParticleComponent(int emitterHandle, MaterialId material)
{
    public int EmitterHandle = emitterHandle;
    public MaterialId Material = material;
}