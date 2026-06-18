using System.Numerics;
using System.Text.Json.Serialization;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Assets.Descriptors;

public sealed class MaterialStateRecord
{
    [JsonInclude] public GfxDrawFlags EnableFlags = GfxDrawFlags.None;
    [JsonInclude] public GfxDrawFlags DisableFlags = GfxDrawFlags.None;
    [JsonInclude] public GfxDrawFunctions DrawFunctions = default;

    [JsonInclude] public Color4? Color;
    [JsonInclude] public Color4? SpecularColor;
    [JsonInclude] public Vector4? UvTransform;
    [JsonInclude] public float? Shininess;
    [JsonInclude] public float? Roughness;
    [JsonInclude] public float? Metallic;

    public static MaterialStateRecord Make(float specular, float shininess, float uvRepeat = 1f)
    {
        return new MaterialStateRecord()
        {
            Color = Color4.White,
            SpecularColor = Color4.White with { A = specular },
            UvTransform = new Vector4(0, 0, uvRepeat, uvRepeat),
            Shininess = shininess,
            Roughness = 0f,
            Metallic = 0f
        };
    }

    public void WriteTo(MaterialState state)
    {
        if (EnableFlags != GfxDrawFlags.None)
            state.DrawState = state.DrawState.WithEnabled(EnableFlags);

        if (DisableFlags != GfxDrawFlags.None)
            state.DrawState = state.DrawState.WithDisabled(DisableFlags);

        if (Color.HasValue) state.Color = Color.Value;
        if (SpecularColor.HasValue) state.SpecularColor = SpecularColor.Value;
        if (Shininess.HasValue) state.Shininess = Shininess.Value;
        if (UvTransform.HasValue) state.UvTransform = UvTransform.Value;
        if (Roughness.HasValue) state.Roughness = Roughness.Value;
        if (Metallic.HasValue) state.Metallic = Metallic.Value;
    }
}