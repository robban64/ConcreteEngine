using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Renderer.Visuals;

[StructLayout(LayoutKind.Sequential)]
public struct FogHeightParams
{
    public float Density;
    public float Strength;
    public float MaxDistance;
    public float BaseHeight;
    public float HeightFalloff;
}

[StructLayout(LayoutKind.Sequential)]
public struct FogOpticsParams
{
    public Color4 Color;
    public float Scattering;
    public float DistanceWeight;
    public float HeightWeight;
}