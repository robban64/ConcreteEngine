using System.Numerics;
using System.Runtime.InteropServices;

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
public struct AmbientParams(Vector3 ambient, Vector3 ambientGround, float exposure)
{
    public Vector3 Ambient = ambient;
    public Vector3 AmbientGround = ambientGround;
    public float Exposure = exposure;
}