using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Shared.Visuals;

[StructLayout(LayoutKind.Sequential)]
public struct FogParams(
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
    public Vector3 Color = color;
    public float Density = density;
    public float HeightFalloff = heightFalloff;
    public float BaseHeight = baseHeight;
    public float Scattering = scattering;
    public float MaxDistance = maxDistance;
    public float HeightInfluence = heightInfluence;
    public float Strength = strength;
}