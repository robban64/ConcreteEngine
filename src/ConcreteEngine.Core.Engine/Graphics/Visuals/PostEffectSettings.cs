using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics.Visuals;
// @formatter:off


[Inspect]
public sealed class PostEffectSettings : VisualStateObject
{
    [InspectInclude]
    public PostGradeParams Grade
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(1.0f, 1.1f, 1.05f, 0.0f);

    [InspectInclude]
    public PostWhiteBalanceParams WhiteBalance
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(0.0f, 0.0f);

    [InspectInclude]
    public PostBloomParams Bloom
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(0.5f, 0.85f, 3.0f);

    [InspectInclude]
    public PostImageFxParams ImageFx
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(0.25f, 0.15f, 0.20f, 0.0f);
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