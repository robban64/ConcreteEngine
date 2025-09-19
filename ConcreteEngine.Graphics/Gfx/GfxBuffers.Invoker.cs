using ConcreteEngine.Graphics.Gfx.Internal;

namespace ConcreteEngine.Graphics.Gfx;

internal sealed class GfxBuffersInvoker
{
    private readonly IGraphicsDriver _driver;

    internal GfxBuffersInvoker(GfxContext context)
    {
        _driver = context.Driver;
    }
}