#region

using System.Runtime.InteropServices;

#endregion

namespace ConcreteEngine.Shared.Rendering;

[StructLayout(LayoutKind.Sequential)]
public struct WorldParamsData
{
    public SunLightParams SunLight;
    public AmbientParams Ambient;
    public FogParams Fog;
    public ShadowParams Shadow;
    public PostEffectParams PostEffect;
}