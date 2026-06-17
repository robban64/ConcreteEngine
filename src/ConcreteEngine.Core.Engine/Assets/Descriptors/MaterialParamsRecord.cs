using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Assets.Descriptors;

public sealed class MaterialParamsRecord
{
    public Color4? Color  { get; init; }
    public Color4? SpecularColor  { get; init; }
    public Vector4? UvTransform  { get; init; }
    public float? Shininess  { get; init; }
    public float? Roughness  { get; init; }
    public float? Metallic { get; init; }

    public void WriteTo(MaterialState state)
    {
        if (Color is { } color) state.Albedo = color;
        if (SpecularColor is { } specColor) state.SpecularColor = specColor;
        if (Shininess is { } shininess) state.Shininess = shininess;
        if (UvTransform is { } uvTransform) state.UvTransform = uvTransform;
        if (Roughness is { } roughness) state.Roughness = roughness;
        if (Metallic is { } metallic) state.Metallic = metallic;
    }

}