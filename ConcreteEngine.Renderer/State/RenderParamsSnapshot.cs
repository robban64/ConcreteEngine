#region

using ConcreteEngine.Shared.Rendering;

#endregion

namespace ConcreteEngine.Renderer.State;

public sealed class RenderParamsSnapshot
{
    public long Generation { get; private set; }
    public bool IsDirty { get; private set; } = true;

    private AmbientParams _ambient;
    private FogParams _fog;
    private SunLightParams _sunLight;
    private ShadowParams _shadows;
    private PostEffectParams _postEffect;

    public ref readonly AmbientParams Ambient => ref _ambient;
    public ref readonly FogParams Fog => ref _fog;
    public ref readonly SunLightParams SunLight => ref _sunLight;
    public ref readonly ShadowParams Shadows => ref _shadows;
    public ref readonly PostEffectParams PostEffects => ref _postEffect;

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
        _ambient = ambient;
        _fog = fog;
        _sunLight = sunLight;
        _shadows = shadows;
        _postEffect = postEffect;
    }

    public void ClearDirty() => IsDirty = false;
}