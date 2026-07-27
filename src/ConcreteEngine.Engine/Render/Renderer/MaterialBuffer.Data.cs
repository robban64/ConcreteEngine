using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render.Renderer;

[StructLayout(LayoutKind.Sequential)]
public readonly struct MaterialMeta(
    ShaderId shaderId,
    GfxDrawState drawState,
    GfxDrawFunctions drawFunctions,
    sbyte shadowMapBinding)
{
    public readonly GfxDrawState DrawState = drawState;
    public readonly GfxDrawFunctions DrawFunctions = drawFunctions;
    public readonly ShaderId ShaderId = shaderId;
    public readonly sbyte ShadowMapBinding = shadowMapBinding;
}