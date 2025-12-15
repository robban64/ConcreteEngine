using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Worlds.MeshGeneration;

public interface IRenderBatcher : IDisposable;

public abstract class MeshGenerator(GfxContext gfx) : IRenderBatcher
{
    protected readonly GfxContext Gfx = gfx;
    public abstract void Dispose();
}