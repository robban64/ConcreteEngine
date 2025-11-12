#region

using System.Numerics;
using System.Runtime.InteropServices;

#endregion

namespace ConcreteEngine.Shared.RenderData;


[StructLayout(LayoutKind.Sequential)]
public readonly struct DirLightParams(Vector3 direction, Vector3 diffuse, float intensity, float specular)
{
    public readonly Vector3 Direction  = direction;
    public readonly Vector3 Diffuse  = diffuse;
    public readonly float Intensity  = intensity;
    public readonly float Specular  = specular;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct AmbientParams(Vector3 ambient, Vector3 ambientGround, float exposure)
{
    public readonly Vector3 Ambient  = ambient;
    public readonly Vector3 AmbientGround  = ambientGround;
    public readonly float Exposure  = exposure;


}

