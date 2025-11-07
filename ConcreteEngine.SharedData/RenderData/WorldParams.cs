using System.Numerics;

namespace ConcreteEngine.Shared.RenderData;

public readonly record struct DirLightParams(Vector3 Direction, Vector3 Diffuse, float Intensity, float Specular);

public readonly record struct AmbientParams(Vector3 Ambient, Vector3 AmbientGround, float Exposure);

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
