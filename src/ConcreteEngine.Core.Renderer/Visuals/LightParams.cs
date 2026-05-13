using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Renderer.Visuals;

[StructLayout(LayoutKind.Sequential)]
public struct DirLightParams(Vector3 direction, Vector3 diffuse, float intensity, float specular)
{
    public Vector3 Direction = direction;
    public Vector3 Diffuse = diffuse;
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