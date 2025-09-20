using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx.Internal;

internal sealed class GfxContextInternal(IGraphicsDriver driver, GfxResourceRepository repositories, FrontendStoreHub stores)
{
    public IGraphicsDriver Driver { get; } = driver;
    public GfxResourceRepository Repositories { get; } = repositories;
    public FrontendStoreHub Stores { get; } = stores;

}