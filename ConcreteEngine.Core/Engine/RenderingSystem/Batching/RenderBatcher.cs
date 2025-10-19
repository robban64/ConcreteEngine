#region

using ConcreteEngine.Graphics.Gfx;

#endregion

namespace ConcreteEngine.Core.Engine.RenderingSystem.Batching;

public interface IRenderBatcher : IDisposable;

public abstract class RenderBatcher<TBatchData> : IRenderBatcher where TBatchData : unmanaged
{
    protected readonly GfxContext Gfx;

    protected RenderBatcher(GfxContext gfx)
    {
        Gfx = gfx;
    }

    public abstract TBatchData BuildBatch();

    public abstract void Dispose();
}