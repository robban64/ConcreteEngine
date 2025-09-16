#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering;

public interface IRenderBatcher : IDisposable;

public abstract class RenderBatcher<TBatchData> : IRenderBatcher where TBatchData : unmanaged
{
    private readonly IGraphicsRuntime _graphics;
    protected IGraphicsContext Gfx => _graphics.Context;
    protected IGfxResourceAllocator Allocator => _graphics.Allocator;
    protected IGfxFactoryHub FactoryHub => _graphics.FactoryHub;
    protected IGfxResourceDisposer GfxDisposer => _graphics.Disposer;

    protected RenderBatcher(IGraphicsRuntime graphics)
    {
        _graphics = graphics;
    }

    public abstract TBatchData BuildBatch();

    public abstract void Dispose();
}