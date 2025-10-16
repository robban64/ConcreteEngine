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


[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawMaterialPayload(MaterialId materialId, ShaderId shaderId, in MaterialParams param)
{
    public readonly MaterialId MaterialId = materialId;
    public readonly ShaderId ShaderId = shaderId;
    public readonly MaterialParams MatParams = param;
}