using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderObjectManager
{
    public readonly MeshGeneratorRegistry MeshRegistry;
    public readonly TerrainSystem TerrainSystem;
    public readonly ParticleSystem Particles;
    public readonly AnimationTable Animations;

    internal RenderObjectManager(GraphicsRuntime graphics)
    {
        MeshRegistry = new MeshGeneratorRegistry(graphics.Gfx);
        TerrainSystem = new TerrainSystem(MeshRegistry.Terrain);
        Particles = new ParticleSystem(MeshRegistry.Particle);
        Animations = new AnimationTable();
    }
}