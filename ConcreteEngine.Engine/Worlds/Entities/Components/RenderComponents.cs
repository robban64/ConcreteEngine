using System.Runtime.InteropServices;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Worlds.Entities.Components;

public enum RenderSourceKind
{
    Model,
    Particle,
}

[StructLayout(LayoutKind.Sequential)]
public struct RenderSourceComponent(ModelId model, int drawCount, MaterialTagKey materialTagKey, RenderSourceKind kind =  RenderSourceKind.Model)
{
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialTagKey;
    public int DrawCount = drawCount;
    public RenderSourceKind  Kind = kind;
}

[StructLayout(LayoutKind.Sequential)]
public struct ParticleComponent(int emitterHandle, MaterialId material)
{
    public int EmitterHandle = emitterHandle;
    public MaterialId Material = material;
}