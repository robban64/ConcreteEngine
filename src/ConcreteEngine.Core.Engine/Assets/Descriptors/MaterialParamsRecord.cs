using System.Numerics;
using System.Text.Json.Serialization;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Assets.Descriptors;

public sealed class MaterialParamsRecord
{
    [JsonInclude] public Color4? Color;
    [JsonInclude]public Color4? SpecularColor;
    [JsonInclude]public Vector4? UvTransform;
    [JsonInclude]public float? Shininess;
    [JsonInclude]public float? Roughness;
    [JsonInclude]public float? Metallic;

    public void WriteTo(MaterialState state)
    {
        if (Color is { } color) state.Color = color;
        if (SpecularColor is { } specColor) state.SpecularColor = specColor;
        if (Shininess is { } shininess) state.Shininess = shininess;
        if (UvTransform is { } uvTransform) state.UvTransform = uvTransform;
        if (Roughness is { } roughness) state.Roughness = roughness;
        if (Metallic is { } metallic) state.Metallic = metallic;
    }

}