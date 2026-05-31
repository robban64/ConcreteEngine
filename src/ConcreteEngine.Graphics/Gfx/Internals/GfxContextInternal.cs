using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx.Internals;

internal sealed class GfxContextInternal(
    GlBackendDriver driver,
    GfxResourceManager resources,
    GfxResourceDisposer disposer)
{
    public GlBackendDriver Driver { get; } = driver;

    public GfxResourceManager Resources { get; } = resources;
    public GfxResourceDisposer Disposer { get; } = disposer;
}