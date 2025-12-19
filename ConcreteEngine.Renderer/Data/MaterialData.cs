using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Gfx.Resources.Handles;

namespace ConcreteEngine.Renderer.Data;

[StructLayout(LayoutKind.Sequential)]
public readonly struct RenderMaterialMeta(
    MaterialId materialId,
    ShaderId shaderId,
    GfxPassState passState,
    GfxPassStateFunc passStateFunc)
{
    public readonly MaterialId MaterialId = materialId;
    public readonly ShaderId ShaderId = shaderId;
    public readonly GfxPassState PassState = passState;
    public readonly GfxPassStateFunc PassStateFunc = passStateFunc;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct RenderMaterialPayload(in RenderMaterialMeta meta, in RenderMaterialData param)
{
    public readonly RenderMaterialMeta Meta = meta;
    public readonly RenderMaterialData MatParams = param;
}