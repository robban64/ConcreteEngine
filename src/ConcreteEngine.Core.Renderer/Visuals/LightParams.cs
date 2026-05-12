using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Renderer.Visuals;

[StructLayout(LayoutKind.Sequential)]
public struct SunLightParams(Vector3 direction, Vector3 diffuse, float intensity, float specular)
{
    public Vector3 Direction = direction;
    public Vector3 Diffuse = diffuse;
    public float Intensity = intensity;
    public float Specular = specular;
}

[StructLayout(LayoutKind.Sequential)]
public struct DirectionalLightParams(Color4 direction, Color4 diffuse, float intensity, float specular)
{
    public Color4 Direction = direction;
    public Color4 Diffuse = diffuse;
    public float Intensity = intensity;
    public float Specular = specular;
}


[StructLayout(LayoutKind.Sequential)]
public struct AmbientParams(Color4 ambient, Color4 ambientGround, float exposure)
{
    public Color4 Ambient = ambient;
    public Color4 AmbientGround = ambientGround;
    public float Exposure = exposure;
}