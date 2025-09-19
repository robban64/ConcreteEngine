using ConcreteEngine.Graphics.Gfx.Internal;

namespace ConcreteEngine.Graphics.Gfx;

internal sealed class GfxMeshesInvoker
{
    private readonly IGraphicsDriver _driver;

    internal GfxMeshesInvoker(GfxContext context)
    {
        _driver = context.Driver;
    }
}