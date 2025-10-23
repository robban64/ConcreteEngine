#region

using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Internal;

internal sealed class GfxContextInternal(
    IGraphicsDriver driver,
    GfxResourceRepository repositories,
    GfxStoreHub stores,
    GfxResourceDisposer disposer)
{
    public IGraphicsDriver Driver { get; } = driver;
    public GfxResourceRepository Repositories { get; } = repositories;
    public GfxStoreHub Stores { get; } = stores;
    public GfxResourceDisposer Disposer { get; } = disposer;
}