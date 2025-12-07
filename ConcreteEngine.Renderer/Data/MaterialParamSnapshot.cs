#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Shared.Rendering;

#endregion

namespace ConcreteEngine.Renderer.Data;

[StructLayout(LayoutKind.Sequential)]
public readonly struct MaterialParamSnapshot
{
    public readonly Color4 Color;

    public float Specular { get; init; }
    public float SpecularFactor { get; init; }
    public float Shininess { get; init; }
    public float UvRepeat { get; init; }

    public float TransparencyToggle { get; init; }
    public float NormalToggle { get; init; }
    public float AlphaToggle { get; init; }

    public bool IsTransparent => TransparencyToggle > 0.1f;
    public bool HasNormal => NormalToggle > 0.1f;
    public bool HasAlpha => AlphaToggle > 0.1f;


    public MaterialParamSnapshot(
        in Color4 color,
        float specular,
        float shininess,
        float uvRepeat = 1f,
        bool transparent = false,
        bool hasNormal = false,
        bool hasAlpha = false)
    {
        Color = color;
        Specular = specular;
        Shininess = shininess;
        UvRepeat = uvRepeat;
        TransparencyToggle = transparent ? 1f : 0f;
        NormalToggle = hasNormal ? 1f : 0f;
        AlphaToggle = hasAlpha ? 1f : 0f;
    }

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

    public static MaterialParamSnapshot From(ref MaterialParameters param)
    {
        return new MaterialParamSnapshot(in param.Color, param.Specular, param.Shininess, param.UvRepeat);
    }
}