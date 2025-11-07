namespace ConcreteEngine.Shared.RenderData;

// -1..+1 > -0.10..+0.10 
// 0..1 > 0.8–1.2
// -1..+1 > -0.05..+0.05
// 0..1
public readonly record struct PostImageFxParams(float Vignette, float Grain, float Sharpen, float Rolloff);

// 0..1 
// 0..1 > 0.6–0.9
public readonly record struct PostBloomParams(float Intensity, float Threshold, float Radius);

// 0..1 > 0.9–1.1 // -1..+1 > -0.05..+0.05
public readonly record struct PostWhiteBalanceParams(float Tint, float Strength);

// 0..1 > 0..0.15 // 0..1 > 0..0.01 // 0..1 > 0..0.15 // 0..1 > 0..0.12
public readonly record struct PostGradeParams(float Exposure, float Saturation, float Contrast, float Warmth);