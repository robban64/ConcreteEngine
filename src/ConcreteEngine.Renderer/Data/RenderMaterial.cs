using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Renderer.Data;

[StructLayout(LayoutKind.Sequential)]
public readonly struct RenderMaterialPayload(in RenderMaterialMeta meta, in RenderMaterial material)
{
    public readonly RenderMaterial Material = material;
    public readonly RenderMaterialMeta Meta = meta;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct RenderMaterialMeta(
    MaterialId materialId,
    ShaderId shaderId,
    GfxPassState passState,
    GfxPassStateFunc passStateFunc)
{
    public readonly ShaderId ShaderId = shaderId;
    public readonly MaterialId MaterialId = materialId;
    public readonly GfxPassState PassState = passState;
    public readonly GfxPassStateFunc PassStateFunc = passStateFunc;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct RenderMaterial(
    in Color4 color,
    float specular,
    float shininess,
    float uvRepeat = 1f,
    bool transparent = false,
    bool hasNormal = false,
    bool hasAlpha = false)
{
    public readonly Color4 Color = color;

    public readonly float Specular = specular;
    public readonly float SpecularFactor;
    public readonly float Shininess = shininess;
    public readonly float UvRepeat = uvRepeat;

    public readonly float TransparencyToggle = transparent ? 1f : 0f;
    public readonly float NormalToggle = hasNormal ? 1f : 0f;
    public readonly float AlphaToggle = hasAlpha ? 1f : 0f;

    public bool HasTransparency => TransparencyToggle > 0.1f;
    public bool HasNormal => NormalToggle > 0.1f;
    public bool HasAlpha => AlphaToggle > 0.1f;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteTo(ref MaterialUniformRecord record)
    {
        record.MatColor = Color.AsVec4();
        record.MatParams0 = new Vector4(Specular, UvRepeat, 1.0f, 1.0f);
        record.MatParams1 = new Vector4(Shininess, NormalToggle, TransparencyToggle, AlphaToggle);
    }
}