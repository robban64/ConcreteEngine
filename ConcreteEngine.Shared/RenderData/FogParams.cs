using System.Numerics;

namespace ConcreteEngine.Shared.RenderData;

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