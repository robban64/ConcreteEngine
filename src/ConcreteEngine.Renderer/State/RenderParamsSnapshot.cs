using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Visuals;

namespace ConcreteEngine.Renderer.State;

public sealed class RenderParamsSnapshot
{
    public bool IsDirty = false;

    public Size2D ScreenFboSize;
    public AmbientParams Ambient;
    public FogParams Fog;
    public SunLightParams SunLight;
    public ShadowParams Shadow;
    public PostEffectParams PostEffect;
}