using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

internal sealed class GfxBuffers
{
    private readonly FrontendStoreHub _resources;
    private readonly GfxResourceRepository _repository;
    
    private readonly GfxBuffersInvoker _invoker;

    internal GfxBuffers(GfxContext context)
    {
        _invoker = new GfxBuffersInvoker(context);
        _resources = context.Stores;
        _repository = context.Repositories;
    }

}