using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Rendering;

public abstract class RenderBatcher<T>: IDisposable where T : unmanaged
{
    protected readonly IGraphicsDevice Graphics;
    
    protected RenderBatcher(IGraphicsDevice graphics)
    {
        Graphics = graphics;
    }
    
    public abstract T BuildBatch();
    
    public abstract void Dispose();
}