using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Rendering;

public abstract class RenderBatcher<TBatchData>: IDisposable where TBatchData : unmanaged
{
    protected readonly IGraphicsDevice Graphics;
    
    protected RenderBatcher(IGraphicsDevice graphics)
    {
        Graphics = graphics;
    }
    
    public abstract TBatchData BuildBatch();
    
    public abstract void Dispose();
}