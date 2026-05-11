using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Mesh;

internal abstract class MeshGenerator(GfxContext gfx) : IDisposable
{
    protected readonly GfxContext Gfx = gfx;
    public abstract void Dispose();
}