#region

using System.Runtime.InteropServices;

#endregion

namespace ConcreteEngine.Shared.RenderData;

[StructLayout(LayoutKind.Sequential)]
public struct PostEffectParams(
    in PostGradeParams grade,
    in PostWhiteBalanceParams whiteBalance,
    in PostBloomParams bloom,
    in PostImageFxParams imageFx
)
{
    public PostGradeParams Grade = grade;
    public PostWhiteBalanceParams WhiteBalance = whiteBalance;
    public PostBloomParams Bloom = bloom;
    public PostImageFxParams ImageFx = imageFx;

    public void Deconstruct(out PostGradeParams grade, out PostWhiteBalanceParams whiteBalance,
        out PostBloomParams bloom, out PostImageFxParams imageFx)
    {
        grade = Grade;
        whiteBalance = WhiteBalance;
        bloom = Bloom;
        imageFx = ImageFx;
    }
}

// -1..+1 > -0.10..+0.10 
// 0..1 > 0.8–1.2
// -1..+1 > -0.05..+0.05
// 0..1
[StructLayout(LayoutKind.Sequential)]
public readonly struct PostImageFxParams(float vignette, float grain, float sharpen, float rolloff)
{
    public readonly float Vignette = vignette;
    public readonly float Grain = grain;
    public readonly float Sharpen = sharpen;
    public readonly float Rolloff = rolloff;
}

// 0..1 
// 0..1 > 0.6–0.9
[StructLayout(LayoutKind.Sequential)]
public readonly struct PostBloomParams(float intensity, float threshold, float radius)
{
    public readonly float Intensity = intensity;
    public readonly float Threshold = threshold;
    public readonly float Radius = radius;
}

// 0..1 > 0.9–1.1 // -1..+1 > -0.05..+0.05
[StructLayout(LayoutKind.Sequential)]
public readonly struct PostWhiteBalanceParams(float tint, float strength)
{
    public readonly float Tint = tint;
    public readonly float Strength = strength;
}

// 0..1 > 0..0.15 // 0..1 > 0..0.01 // 0..1 > 0..0.15 // 0..1 > 0..0.12
[StructLayout(LayoutKind.Sequential)]
public readonly struct PostGradeParams(float exposure, float saturation, float contrast, float warmth)
{
    public readonly float Exposure = exposure;
    public readonly float Saturation = saturation;
    public readonly float Contrast = contrast;
    public readonly float Warmth = warmth;
}