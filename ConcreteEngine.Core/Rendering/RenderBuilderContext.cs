using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Rendering;

public sealed class RenderBuilderContext
{
    internal RenderBuilderContext(GfxContext gfx, Size2D outputSize)
    {
        Gfx = gfx;
        OutputSize = outputSize;
    }

    public GfxContext Gfx { get; }
    public Size2D OutputSize { get; }
    public bool Done { get; private set; }
}