using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Renderer.Buffer;

[StructLayout(LayoutKind.Sequential)]
public readonly struct RenderMaterialMeta(
    ShaderId shaderId,
    GfxDrawState drawState,
    GfxPassFunctions passFunctions,
    sbyte shadowMapBinding)
{
    public readonly GfxDrawState DrawState = drawState;
    public readonly GfxPassFunctions PassFunctions = passFunctions;
    public readonly ShaderId ShaderId = shaderId;
    public readonly sbyte ShadowMapBinding = shadowMapBinding;
}