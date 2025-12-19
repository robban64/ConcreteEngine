using ConcreteEngine.Shared.Rendering;

namespace ConcreteEngine.Renderer.State;

public sealed class RenderParamsSnapshot
{
    public long Generation;
    public bool IsDirty = true;

    public AmbientParams Ambient;
    public FogParams Fog;
    public SunLightParams SunLight;
    public ShadowParams Shadows;
    public PostEffectParams PostEffect;

}