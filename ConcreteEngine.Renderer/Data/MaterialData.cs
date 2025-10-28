#region

using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Renderer.Data;

public readonly record struct MaterialParams(
    Color4 Color,
    float Specular,
    float Shininess,
    float UvRepeat = 1f,
    bool HasNormal = false,
    bool HasAlpha = false);

public readonly record struct DrawMaterialMeta(MaterialId MaterialId, ShaderId ShaderId, GfxPassState? PassState, GfxPassStateFunc? PassStateFunc);

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawMaterialPayload(in DrawMaterialMeta meta, in MaterialParams param)
{
    public readonly DrawMaterialMeta Meta = meta;
    public readonly MaterialParams MatParams = param;
}