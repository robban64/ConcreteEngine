using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Shared.RenderData;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FogParams(
    Vector3 color,
    float density,
    float heightFalloff,
    float baseHeight,
    float scattering,
    float maxDistance,
    float heightInfluence,
    float strength
)
{
    public readonly Vector3 Color = color;
    public readonly float Density = density;
    public readonly float HeightFalloff = heightFalloff;
    public readonly float BaseHeight = baseHeight;
    public readonly float Scattering = scattering;
    public readonly float MaxDistance = maxDistance;
    public readonly float HeightInfluence = heightInfluence;
    public readonly float Strength = strength;

}