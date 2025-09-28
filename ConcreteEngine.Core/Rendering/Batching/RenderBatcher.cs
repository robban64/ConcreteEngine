#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering;

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