using System.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Shared.RenderData;

namespace ConcreteEngine.Editor.DataState;

public struct WorldParamState
{
    public LightState LightState;
    public FogState FogState;
    public PostEffectState PostState;
}

public struct LightState
{
    public DirLightState DirectionalLight;
    public AmbientState AmbientLight;
}

public struct DirLightState(in DirLightParams param)
{
    public Vector3 Direction = param.Direction;
    public Vector3 Diffuse = param.Diffuse;
    public float Intensity = param.Intensity;
    public float Specular = param.Specular;
}

public struct AmbientState(in AmbientParams param)
{
    public Vector3 Ambient = param.Ambient;
    public Vector3 AmbientGround = param.AmbientGround;
    public float Exposure = param.Exposure;
}


public struct FogState(in FogParams param)
{
    public Vector3 Color = param.Color;
    public float Density = param.Density;
    public float HeightFalloff = param.HeightFalloff;
    public float BaseHeight = param.BaseHeight;
    public float Scattering = param.Scattering;
    public float MaxDistance = param.MaxDistance;
    public float HeightInfluence = param.HeightInfluence;
    public float Strength = param.Strength;
}

public struct PostEffectState
{
    public PostGradeState Grade;
    public PostWhiteBalanceState WhiteBalance;
    public PostBloomState Bloom;
    public PostImageFxState ImageFx;
}

public struct PostImageFxState(in PostImageFxParams param)
{
    public float Vignette = param.Vignette;
    public float Grain = param.Grain;
    public float Sharpen = param.Sharpen;
    public float Rolloff = param.Rolloff;
}

public struct PostBloomState(in PostBloomParams param)
{
    public float Intensity = param.Intensity;
    public float Threshold = param.Threshold;
    public float Radius = param.Radius;
}

public struct PostWhiteBalanceState(PostWhiteBalanceParams param)
{
    public float Tint = param.Tint;
    public float Strength = param.Strength;
}

public struct PostGradeState(in PostGradeParams param)
{
    public float Exposure = param.Exposure;
    public float Saturation = param.Saturation;
    public float Contrast = param.Contrast;
    public float Warmth = param.Warmth;
}