using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Mesh;

public abstract class MeshGenerator(GfxContext gfx) : IDisposable
{
    protected readonly GfxContext Gfx = gfx;
    public abstract void Dispose();
}