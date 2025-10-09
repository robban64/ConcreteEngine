using System.Numerics;
using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Core.Rendering.Data;

public sealed class RenderSceneState
{
    public long Version { get; private set; }

    private AmbientParams _ambient;
    private FogParams _fog;
    private SkyboxParams _skybox;
    private DirLightParams _dirLight;
    private ShadowParams _shadows;
    private PostEffectParams _postEffect;

    public ref readonly AmbientParams Ambient => ref _ambient;
    public ref readonly FogParams Fog => ref _fog;
    public ref readonly SkyboxParams Skybox => ref _skybox;
    public ref readonly DirLightParams DirLight => ref _dirLight;
    public ref readonly ShadowParams Shadows => ref _shadows;
    public ref readonly PostEffectParams PostEffects => ref _postEffect;

    internal void Update(
        long version,
        in AmbientParams ambient,
        in FogParams fog,
        in SkyboxParams skybox,
        in DirLightParams dirLight,
        in ShadowParams shadows,
        in PostEffectParams postEffect)
    {
        Version = version;
        _ambient = ambient;
        _fog = fog;
        _skybox = skybox;
        _dirLight = dirLight;
        _shadows = shadows;
        _postEffect = postEffect;
    }
}