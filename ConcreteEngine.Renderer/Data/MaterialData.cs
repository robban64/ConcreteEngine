#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Renderer.Data;


[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawMaterialMeta(
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
public readonly struct DrawMaterialPayload(in DrawMaterialMeta meta, in MaterialParamSnapshot param)
{
    public readonly DrawMaterialMeta Meta = meta;
    public readonly MaterialParamSnapshot MatParams = param;
}