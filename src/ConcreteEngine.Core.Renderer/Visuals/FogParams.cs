using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Renderer.Visuals;

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

public struct FogHeightParams
{
    public float Density;
    public float Strength;
    public float MaxDistance;
    public float BaseHeight;
    public float HeightFalloff;
    
}

public struct FogOpticsParams
{
    public Color4 Color;
    public float Scattering;
    public float DistanceWeight;
    public float HeightWeight;

}