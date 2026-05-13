using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderObjectManager
{
    public readonly TerrainManager TerrainManager;
    public readonly ParticleManager Particles;
    public readonly AnimationTable Animations;

    internal RenderObjectManager(GraphicsRuntime graphics)
    {
        TerrainManager = new TerrainManager(graphics.Gfx);
        Particles = new ParticleManager(graphics.Gfx);
        Animations = new AnimationTable();
    }
}