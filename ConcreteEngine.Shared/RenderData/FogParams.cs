using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Shared.RenderData;

[StructLayout(LayoutKind.Sequential)]
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