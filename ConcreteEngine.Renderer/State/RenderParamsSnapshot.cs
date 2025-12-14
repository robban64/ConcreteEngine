#region

using ConcreteEngine.Shared.Rendering;

#endregion

namespace ConcreteEngine.Renderer.State;

public sealed class RenderParamsSnapshot
{
    public long Generation { get; private set; }
    public bool IsDirty { get; private set; } = true;

    public AmbientParams Ambient;
    public FogParams Fog;
    public SunLightParams SunLight;
    public ShadowParams Shadows;
    public PostEffectParams PostEffect;

    public void Update(
        long version,
        in AmbientParams ambient,
        in FogParams fog,
        in SunLightParams sunLight,
        in ShadowParams shadows,
        in PostEffectParams postEffect)
    {
        IsDirty = true;

        Generation = version;
        Ambient = ambient;
        Fog = fog;
        SunLight = sunLight;
        Shadows = shadows;
        PostEffect = postEffect;
    }

    public void ClearDirty() => IsDirty = false;
}