#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Shared.RenderData;

#endregion

namespace ConcreteEngine.Editor.DataState;

[StructLayout(LayoutKind.Sequential)]
public struct WorldParamState
{
    public LightState LightState;
    public FogState FogState;
    public PostEffectState PostState;
}

[StructLayout(LayoutKind.Sequential)]
public struct LightState
{
    public DirLightState DirectionalLight;
    public AmbientState AmbientLight;
}

[StructLayout(LayoutKind.Sequential)]
public struct DirLightState(in DirLightParams param)
{
    public Vector3 Direction = param.Direction;
    public Vector3 Diffuse = param.Diffuse;
    public float Intensity = param.Intensity;
    public float Specular = param.Specular;

    public void Fill(out DirLightParams result) => result = Unsafe.As<DirLightState, DirLightParams>(ref this);
}

[StructLayout(LayoutKind.Sequential)]
public struct AmbientState(in AmbientParams param)
{
    public Vector3 Ambient = param.Ambient;
    public Vector3 AmbientGround = param.AmbientGround;
    public float Exposure = param.Exposure;
}

[StructLayout(LayoutKind.Sequential)]
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

[StructLayout(LayoutKind.Sequential)]
public struct PostEffectState
{
    public PostGradeState Grade;
    public PostWhiteBalanceState WhiteBalance;
    public PostBloomState Bloom;
    public PostImageFxState ImageFx;
}

[StructLayout(LayoutKind.Sequential)]
public struct PostImageFxState(in PostImageFxParams param)
{
    public float Vignette = param.Vignette;
    public float Grain = param.Grain;
    public float Sharpen = param.Sharpen;
    public float Rolloff = param.Rolloff;
}

[StructLayout(LayoutKind.Sequential)]
public struct PostBloomState(in PostBloomParams param)
{
    public float Intensity = param.Intensity;
    public float Threshold = param.Threshold;
    public float Radius = param.Radius;
}

[StructLayout(LayoutKind.Sequential)]
public struct PostWhiteBalanceState(PostWhiteBalanceParams param)
{
    public float Tint = param.Tint;
    public float Strength = param.Strength;
}

[StructLayout(LayoutKind.Sequential)]
public struct PostGradeState(in PostGradeParams param)
{
    public float Exposure = param.Exposure;
    public float Saturation = param.Saturation;
    public float Contrast = param.Contrast;
    public float Warmth = param.Warmth;
}