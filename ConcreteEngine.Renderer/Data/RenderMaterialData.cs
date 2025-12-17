using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Shared.Rendering;

namespace ConcreteEngine.Renderer.Data;

[StructLayout(LayoutKind.Sequential)]
public readonly struct RenderMaterialData(
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
    public void Fill(out Vector4 color, out Vector4 param1, out Vector4 param2)
    {
        color = Color.AsVec4();
        param1 = new Vector4(Specular, UvRepeat, 1.0f, 1.0f);
        param2 = new Vector4(Shininess, NormalToggle, TransparencyToggle, AlphaToggle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteTo(ref MaterialUniformRecord record)
    {
        record.MatColor = Color.AsVec4();
        record.MatParams0 = new Vector4(Specular, UvRepeat, 1.0f, 1.0f);
        record.MatParams1 = new Vector4(Shininess, NormalToggle, TransparencyToggle, AlphaToggle);
    }

    public static RenderMaterialData From(ref MaterialParameters param)
    {
        return new RenderMaterialData(in param.Color, param.Specular, param.Shininess, param.UvRepeat);
    }
}