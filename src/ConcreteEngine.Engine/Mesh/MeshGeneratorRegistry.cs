using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Mesh;

internal abstract class MeshGenerator(GfxContext gfx) : IDisposable
{
    protected readonly GfxContext Gfx = gfx;
    public abstract void Dispose();
}

internal sealed class MeshGeneratorRegistry(GfxContext gfx)
{
    public readonly ParticleMesh Particle = new(gfx);
    public readonly TerrainMesh Terrain = new(gfx);
}