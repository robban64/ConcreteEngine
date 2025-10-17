using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Core.Rendering.Data;

public readonly record struct MaterialParams(
    Color4 Color,
    float Specular,
    float Shininess,
    float UvRepeat = 1f,
    // todo remove
    float Normal = 1f);


public readonly record struct DrawMaterialMeta(MaterialId MaterialId, ShaderId ShaderId);


[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawMaterialPayload(in DrawMaterialMeta meta, in MaterialParams param)
{
    public readonly DrawMaterialMeta Meta =  meta;
    public readonly MaterialParams MatParams = param;
}
