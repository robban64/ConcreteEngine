using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx.Internals;

internal sealed class GfxContextInternal(GfxResourceManager resources, GfxResourceDisposer disposer)
{
    public GfxResourceManager Resources { get; } = resources;
    public GfxResourceDisposer Disposer { get; } = disposer;
}