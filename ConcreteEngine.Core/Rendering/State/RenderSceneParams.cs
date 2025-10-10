#region

using System.Numerics;
using ConcreteEngine.Core.Assets.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.State;

public readonly record struct SkyboxParams(MaterialId MaterialId, Quaternion Rotation, float Intensity);

public readonly record struct DirLightParams(Vector3 Direction, Vector3 Diffuse, float Intensity, float Specular);

public readonly record struct AmbientParams(Vector3 Ambient, Vector3 AmbientGround, float Exposure);

public readonly record struct FogParams(
    Vector3 Color,
    float Density,
    float HeightFalloff,
    float BaseHeight,
    float Scattering,
    float MaxDistance,
    float HeightInfluence,
    float Strength
);

public readonly record struct ShadowParams(
    int ShadowMapSize,
    float ZPad,
    float ConstBias,
    float SlopeBias,
    float Strength,
    float PcfRadius
);

public readonly record struct PostEffectParams(
    PostEffectParams.GradeInfo Grade,
    PostEffectParams.WhiteBalanceInfo WhiteBalance,
    PostEffectParams.BloomInfo Bloom,
    PostEffectParams.ImageFxInfo ImageFx
)
{
    // -1..+1 > -0.10..+0.10
    // 0..1 > 0.8–1.2
    // 0..1 > 0.9–1.1
    // -1..+1 > -0.05..+0.05
    public readonly record struct GradeInfo(float Exposure, float Saturation, float Contrast, float Warmth);

    // -1..+1 > -0.05..+0.05 // 0..1
    public readonly record struct WhiteBalanceInfo(float Tint, float Strength);

    // 0..1 // 0..1 > 0.6–0.9 // px
    public readonly record struct BloomInfo(float Intensity, float Threshold, float Radius);

    // 0..1 > 0..0.15 // 0..1 > 0..0.01 // 0..1 > 0..0.15 // 0..1 > 0..0.12
    public readonly record struct ImageFxInfo(float Vignette, float Grain, float Sharpen, float Rolloff);
}