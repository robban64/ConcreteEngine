namespace ConcreteEngine.Renderer.State;

public sealed class RenderSceneSnapshot
{
    public long Version { get; private set; }

    private AmbientParams _ambient;
    private FogParams _fog;
    private DirLightParams _dirLight;
    private ShadowParams _shadows;
    private PostEffectParams _postEffect;

    public ref readonly AmbientParams Ambient => ref _ambient;
    public ref readonly FogParams Fog => ref _fog;
    public ref readonly DirLightParams DirLight => ref _dirLight;
    public ref readonly ShadowParams Shadows => ref _shadows;
    public ref readonly PostEffectParams PostEffects => ref _postEffect;

    public void Update(
        long version,
        in AmbientParams ambient,
        in FogParams fog,
        in DirLightParams dirLight,
        in ShadowParams shadows,
        in PostEffectParams postEffect)
    {
        Version = version;
        _ambient = ambient;
        _fog = fog;
        _dirLight = dirLight;
        _shadows = shadows;
        _postEffect = postEffect;
    }
}