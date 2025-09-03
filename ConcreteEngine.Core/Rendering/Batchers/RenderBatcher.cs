#region

using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Rendering;

public interface IRenderBatcher : IDisposable;

public abstract class RenderBatcher<TBatchData> : IRenderBatcher where TBatchData : unmanaged
{
    protected readonly IGraphicsDevice Graphics;

    protected RenderBatcher(IGraphicsDevice graphics)
    {
        Graphics = graphics;
    }

    public abstract TBatchData BuildBatch();

    public abstract void Dispose();
}