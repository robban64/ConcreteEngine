using System.Runtime.InteropServices;

namespace ConcreteEngine.Shared.RenderData;

[StructLayout(LayoutKind.Sequential)]
public struct WorldParamsData
{
    public DirLightParams DirLight;
    public AmbientParams Ambient;
    public FogParams Fog;
    public PostEffectParams PostEffect;
}