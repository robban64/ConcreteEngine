using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render;

internal sealed class RenderContext
{
    public required RenderEntityHub RenderEcs;
    public required GameEntityHub GameEcs;
    
    public required Camera Camera;
    
    public required MeshTable MeshTable;
    public required MaterialTable MaterialTable;
    public required AnimationTable AnimationTable;
    public required ParticleSystem ParticleSystem;
}