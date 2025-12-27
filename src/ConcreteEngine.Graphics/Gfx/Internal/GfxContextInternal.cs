namespace ConcreteEngine.Graphics.Gfx.Internal;

internal sealed class GfxContextInternal(
    IGraphicsDriver driver,
    GfxResourceManager resources,
    GfxResourceDisposer disposer)
{
    public IGraphicsDriver Driver { get; } = driver;

    public GfxResourceManager Resources { get; } = resources;
    public GfxResourceDisposer Disposer { get; } = disposer;
}