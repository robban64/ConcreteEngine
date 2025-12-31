using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Specs.Visuals;

[StructLayout(LayoutKind.Sequential)]
public struct WorldParamsData
{
    public SunLightParams SunLight;
    public AmbientParams Ambient;
    public FogParams Fog;
    public ShadowParams Shadow;
    public PostEffectParams PostEffect;
}

public ref struct WorldParamsView
{
    public ref SunLightParams SunLight;
    public ref AmbientParams Ambient;
    public ref FogParams Fog;
    public ref ShadowParams Shadow;
    public ref PostEffectParams PostEffect;
}