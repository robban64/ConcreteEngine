using ConcreteEngine.Core.Specs.Visuals;

namespace ConcreteEngine.Renderer.State;

public sealed class RenderParamsSnapshot
{
    public bool IsDirty = true;

    public AmbientParams Ambient;
    public FogParams Fog;
    public SunLightParams SunLight;
    public ShadowParams Shadow;
    public PostEffectParams PostEffect;
}