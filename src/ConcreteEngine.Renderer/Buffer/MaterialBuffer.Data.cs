using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Renderer.Buffer;

[StructLayout(LayoutKind.Sequential)]
public readonly struct RenderMaterialMeta(
    MaterialId materialId,
    ShaderId shaderId,
    GfxDrawState drawState,
    GfxPassFunctions passFunctions,
    sbyte shadowMapBinding)
{
    public readonly GfxDrawState DrawState = drawState;
    public readonly GfxPassFunctions PassFunctions = passFunctions;
    public readonly ShaderId ShaderId = shaderId;
    public readonly MaterialId MaterialId = materialId;
    public readonly sbyte ShadowMapBinding = shadowMapBinding;
}