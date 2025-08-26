#region

using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Rendering.Batchers;

public abstract class RenderBatcher<TBatchData> : IDisposable where TBatchData : unmanaged
{
    protected readonly IGraphicsDevice Graphics;

    protected RenderBatcher(IGraphicsDevice graphics)
    {
        Graphics = graphics;
    }

    public abstract TBatchData BuildBatch();

    public abstract void Dispose();
}