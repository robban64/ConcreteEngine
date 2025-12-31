using ConcreteEngine.Graphics.OpenGL;

namespace ConcreteEngine.Graphics.Gfx.Internal;

internal sealed class GfxContextInternal(
    GlBackendDriver driver,
    GfxResourceManager resources,
    GfxResourceDisposer disposer)
{
    public GlBackendDriver Driver { get; } = driver;

    public GfxResourceManager Resources { get; } = resources;
    public GfxResourceDisposer Disposer { get; } = disposer;
}