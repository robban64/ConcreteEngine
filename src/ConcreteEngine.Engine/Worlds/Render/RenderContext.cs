using ConcreteEngine.Engine.Worlds.Tables;

namespace ConcreteEngine.Engine.Worlds.Render;

internal sealed class RenderContext
{
    public required Camera Camera;
    public required MeshTable MeshTable;
    public required MaterialTable MaterialTable;
    public required AnimationTable AnimationTable;
    public required ParticleSystem ParticleSystem;
}