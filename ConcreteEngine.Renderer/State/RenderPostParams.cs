namespace ConcreteEngine.Core.Rendering.State;

public readonly record struct PostEffectParams(
    PostGradeParams Grade,
    PostWhiteBalanceParams WhiteBalance,
    PostBloomParams Bloom,
    PostImageFxParams ImageFx
);

public readonly struct PostImageFxParams(float vignette, float grain, float sharpen, float rolloff)
{
    public float Vignette { get; init; } = vignette; // -1..+1 > -0.10..+0.10 
    public float Grain { get; init; } = grain; // 0..1 > 0.8–1.2
    public float Sharpen { get; init; } = sharpen; // -1..+1 > -0.05..+0.05
    public float Rolloff { get; init; } = rolloff; // 0..1
}

public readonly struct PostBloomParams(float intensity, float threshold, float radius)
{
    // 0..1 
    public float Intensity { get; init; } = intensity; // 0..1 > 0.6–0.9
    public float Threshold { get; init; } = threshold;
    public float Radius { get; init; } = radius; // px
}

// 0..1 > 0.9–1.1 // -1..+1 > -0.05..+0.05
public readonly struct PostWhiteBalanceParams(float tint, float strength)
{
    public float Tint { get; init; } = tint;
    public float Strength { get; init; } = strength;
}

// 0..1 > 0..0.15 // 0..1 > 0..0.01 // 0..1 > 0..0.15 // 0..1 > 0..0.12
public readonly struct PostGradeParams(float exposure, float saturation, float contrast, float warmth)
{
    public float Exposure { get; init; } = exposure;
    public float Saturation { get; init; } = saturation;
    public float Contrast { get; init; } = contrast;
    public float Warmth { get; init; } = warmth;
}