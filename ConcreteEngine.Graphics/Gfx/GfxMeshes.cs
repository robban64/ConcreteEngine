using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

internal sealed class GfxMeshes
{
    private readonly FrontendStoreHub _resources;
    private readonly GfxResourceRepository _repository;
    
    private readonly GfxMeshesInvoker _invoker;

    internal GfxMeshes(GfxContext context)
    {
        _invoker = new GfxMeshesInvoker(context);
        _resources = context.Stores;
        _repository = context.Repositories;
    }
}