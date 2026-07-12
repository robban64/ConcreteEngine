using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct DirLightParams(Vector3 direction, Vector3 diffuse, float intensity, float specular)
{
    [InputNumber(Format = "%.3f", Speed = 0.01f, Min = -1f, Max = 1f)]
    public Vector3 Direction = direction;

    [InputColor(HasAlpha = false)] public Vector3 Diffuse = diffuse;

    [InputNumber(InputStyle.Drag, Format = "%.3f", Speed = 0.01f, Min = 0f, Max = 10f)]
    public float Intensity = intensity;

    [InputNumber(InputStyle.Drag, Format = "%.3f", Speed = 0.01f, Min = 0f, Max = 10f)]
    public float Specular = specular;
}

[StructLayout(LayoutKind.Sequential)]
public struct AmbientParams(Vector3 ambient, Vector3 ambientGround, float exposure)
{
    [InputColor(HasAlpha = false)] public Vector3 Ambient = ambient;

    [InputColor(HasAlpha = false)] public Vector3 AmbientGround = ambientGround;

    [InputNumber(InputStyle.Drag, Format = "%.3f", Speed = 0.01f, Min = 0f, Max = 2f)]
    public float Exposure = exposure;
}

[StructLayout(LayoutKind.Sequential)]
public struct ShadowProjectionParams(float distance, float zPad, float constBias, float slopeBias)
{
    [InputNumber(InputStyle.Slider, Min = 10f, Max = 500f)]
    public float Distance = distance;

    [InputNumber(InputStyle.Slider, Min = 10f, Max = 100f)]
    public float ZPad = zPad;

    [InputNumber(InputStyle.Drag, Speed = 0.001f, Min = 0.001f, Max = 0.01f, Format = "%.4f")]
    public float ConstBias = constBias;

    [InputNumber(InputStyle.Drag, Speed = 0.001f, Min = 0.001f, Max = 0.01f, Format = "%.4f")]
    public float SlopeBias = slopeBias;
}

[StructLayout(LayoutKind.Sequential)]
public struct ShadowVisualParams(float strength, float pcfRadius)
{
    [InputNumber(InputStyle.Slider, Min = 0, Max = 1f)]
    public float Strength = strength;

    [InputNumber(InputStyle.Slider, Min = 0.5f, Max = 4f)]
    public float PcfRadius = pcfRadius;
}

[StructLayout(LayoutKind.Sequential)]
public struct FogHeightParams(float density, float strength, float maxDistance, float baseHeight, float heightFalloff)
{
    [InputNumber(InputStyle.Slider, Min = 100, Max = 1500, Format = "%.5f")]
    public float Density = density;

    [InputNumber(InputStyle.Drag, Speed = 0.001f, Min = 0f, Max = 1f, Format = "%.3f")]
    public float Strength = strength;

    [InputNumber(InputStyle.Drag, Speed = 1f, Min = 1f, Max = 10000f, Format = "%.0f")]
    public float MaxDistance = maxDistance;

    [InputNumber(InputStyle.Slider, Min = -1000f, Max = 1000f)]
    public float BaseHeight = baseHeight;

    [InputNumber(InputStyle.Slider, Min = 0.001f, Max = 10000.0f, Format = "%.3f")]
    public float HeightFalloff = heightFalloff;
}

[StructLayout(LayoutKind.Sequential)]
public struct FogOpticsParams(float scattering, float distanceWeight, float heightWeight)
{
    [InputNumber(InputStyle.Drag, Speed = 0.001f, Min = 0f, Max = 1f, Format = "%.5f")]
    public float Scattering = scattering;

    [InputNumber(InputStyle.Drag, Speed = 0.001f, Min = 0f, Max = 1f, Format = "%.0f")]
    public float DistanceWeight = distanceWeight;

    [InputNumber(InputStyle.Drag, Speed = 0.001f, Min = 0f, Max = 1f, Format = "%.0f")]
    public float HeightWeight = heightWeight;
}

// -1..+1 > -0.10..+0.10 
// 0..1 > 0.8–1.2
// -1..+1 > -0.05..+0.05
// 0..1
[StructLayout(LayoutKind.Sequential)]
public struct PostImageFxParams(float vignette, float grain, float sharpen, float rolloff)
{
    [InputNumber(InputStyle.Slider, Min = 0, Max = 0.5f)]
    public float Vignette = vignette;

    [InputNumber(InputStyle.Slider, Min = 0, Max = 0.5f)]
    public float Grain = grain;

    [InputNumber(InputStyle.Slider, Min = 0, Max = 0.5f)]
    public float Sharpen = sharpen;

    [InputNumber(InputStyle.Slider, Min = 0, Max = 0.5f)]
    public float Rolloff = rolloff;
}

// 0..1 
// 0..1 > 0.6–0.9
[StructLayout(LayoutKind.Sequential)]
public struct PostBloomParams(float intensity, float threshold, float radius)
{
    [InputNumber(InputStyle.Slider, Min = 0, Max = 2f)]
    public float Intensity = intensity;

    [InputNumber(InputStyle.Slider, Min = 0, Max = 2f)]
    public float Threshold = threshold;

    [InputNumber(InputStyle.Slider, Min = 0, Max = 10f)]
    public float Radius = radius;
}

// 0..1 > 0.9–1.1 // -1..+1 > -0.05..+0.05
[StructLayout(LayoutKind.Sequential)]
public struct PostWhiteBalanceParams(float tint, float strength)
{
    [InputNumber(InputStyle.Slider, Min = 0, Max = 1f)]
    public float Tint = tint;

    [InputNumber(InputStyle.Slider, Min = -1f, Max = 1f)]
    public float Strength = strength;
}

// 0..1 > 0..0.15 // 0..1 > 0..0.01 // 0..1 > 0..0.15 // 0..1 > 0..0.12
[StructLayout(LayoutKind.Sequential)]
public struct PostGradeParams(float exposure, float saturation, float contrast, float warmth)
{
    [InputNumber(InputStyle.Slider, Min = 0.5f, Max = 2f)]
    public float Exposure = exposure;
    
    [InputNumber(InputStyle.Slider, Min = 0, Max = 1.5f)]
    public float Saturation = saturation;
    
    [InputNumber(InputStyle.Slider, Min = 0, Max = 1.5f)]
    public float Contrast = contrast;
    
    [InputNumber(InputStyle.Slider, Min = 0, Max = 1f)]
    public float Warmth = warmth;
}