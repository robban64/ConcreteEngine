using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Graphics;


[StructLayout(LayoutKind.Sequential)]
public struct DirLightParams(Vector3 direction, Vector3 diffuse, float intensity, float specular)
{
    public Vector3 Direction = direction;
    public Vector3 Diffuse = diffuse;
    public float Intensity = intensity;
    public float Specular = specular;
}

[StructLayout(LayoutKind.Sequential)]
public struct AmbientParams(Color4 ambient, Color4 ambientGround, float exposure)
{
    public Color4 Ambient = ambient;
    public Color4 AmbientGround = ambientGround;
    public float Exposure = exposure;
}

[StructLayout(LayoutKind.Sequential)]
public struct ShadowProjectionParams(float distance, float zPad, float constBias, float slopeBias)
{
    public float Distance = distance;
    public float ZPad = zPad;
    public float ConstBias = constBias;
    public float SlopeBias = slopeBias;
}
[StructLayout(LayoutKind.Sequential)]

public struct ShadowVisualParams(float strength, float pcfRadius)
{
    public float Strength = strength;
    public float PcfRadius = pcfRadius;
}

[StructLayout(LayoutKind.Sequential)]
public struct FogHeightParams
{
    public float Density;
    public float Strength;
    public float MaxDistance;
    public float BaseHeight;
    public float HeightFalloff;
}

[StructLayout(LayoutKind.Sequential)]
public struct FogOpticsParams
{
    public Color4 Color;
    public float Scattering;
    public float DistanceWeight;
    public float HeightWeight;
}


// -1..+1 > -0.10..+0.10 
// 0..1 > 0.8–1.2
// -1..+1 > -0.05..+0.05
// 0..1
[StructLayout(LayoutKind.Sequential)]
public struct PostImageFxParams(float vignette, float grain, float sharpen, float rolloff)
{
    public float Vignette = vignette;
    public float Grain = grain;
    public float Sharpen = sharpen;
    public float Rolloff = rolloff;
}

// 0..1 
// 0..1 > 0.6–0.9
[StructLayout(LayoutKind.Sequential)]
public struct PostBloomParams(float intensity, float threshold, float radius)
{
    public float Intensity = intensity;
    public float Threshold = threshold;
    public float Radius = radius;
}

// 0..1 > 0.9–1.1 // -1..+1 > -0.05..+0.05
[StructLayout(LayoutKind.Sequential)]
public struct PostWhiteBalanceParams(float tint, float strength)
{
    public float Tint = tint;
    public float Strength = strength;
}

// 0..1 > 0..0.15 // 0..1 > 0..0.01 // 0..1 > 0..0.15 // 0..1 > 0..0.12
[StructLayout(LayoutKind.Sequential)]
public struct PostGradeParams(float exposure, float saturation, float contrast, float warmth)
{
    public float Exposure = exposure;
    public float Saturation = saturation;
    public float Contrast = contrast;
    public float Warmth = warmth;
}