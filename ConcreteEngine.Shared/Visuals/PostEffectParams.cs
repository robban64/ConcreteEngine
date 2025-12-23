using System.Runtime.InteropServices;

namespace ConcreteEngine.Shared.Visuals;

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

    public readonly void Deconstruct(out PostGradeParams grade, out PostWhiteBalanceParams whiteBalance,
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