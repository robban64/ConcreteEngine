#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Core.Rendering.Data;

#endregion

namespace ConcreteEngine.Core.Rendering.State;

public sealed class RenderSceneProps
{
    private bool _dirty = true;

    private readonly RenderSceneState _snapshot = new();

    private AmbientParams _ambient = MakeDefaultAmbient();
    private FogParams _fog = MakeDefaultFog();
    private SkyboxParams _skybox;
    private DirLightParams _dirLight = MakeDefaultDirLight();
    private ShadowParams _shadow;
    private PostEffectParams _postEffect = MakeDefaultPostEffect();

    public long Version { get; private set; } = 0;

    internal RenderSceneState Snapshot => _snapshot;

    public void SetSkybox(MaterialId materialId, Quaternion rotation, float intensity = 1f)
    {
        _skybox = new SkyboxParams(materialId, rotation, intensity);
        _dirty = true;
    }

    public void SetDirectionalLight(Vector3 direction, Color4 diffuse, float intensity, float specular)
    {
        _dirLight = new DirLightParams(direction, diffuse.AsVec3(), intensity, specular);
        _dirty = true;
    }

    public void SetAmbient(Color4 ambient, Color4 ambientGround, float exposure = 0)
    {
        _ambient = new AmbientParams(ambient.AsVec3(), ambientGround.AsVec3(), exposure);
        _dirty = true;
    }

    public void SetFog(Color4 color, float density, float heightFalloff, float baseHeight, float scattering,
        float maxDistance, float heightInfluence = 1f, float strength = 1f)
    {
        _fog = new FogParams(
            color: color.AsVec3(),
            density: density,
            heightFalloff: heightFalloff,
            baseHeight: baseHeight,
            scattering: scattering,
            maxDistance: maxDistance,
            heightInfluence: heightInfluence,
            strength: strength);

        _dirty = true;
    }

    public void SetShadowDefault(int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, RenderLimits.MinShadowMapSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, RenderLimits.MaxShadowMapSize);

        var constBias = 0.8f / size;
        var slopeBias = constBias * 6f;
        _shadow = new ShadowParams(
            shadowMapSize: size,
            zPad: 0.5f,
            constBias: constBias,
            slopeBias: slopeBias,
            strength: 1f,
            pcfRadius: 1f);

        _dirty = true;
    }

    public void SetPostEffect(PostEffectParams effect)
    {
        _postEffect = effect;
        _dirty = true;
    }

    internal RenderSceneState Commit()
    {
        if (!_dirty) return _snapshot;

        Version++;

        _snapshot.Update(
            version: Version,
            ambient: in _ambient,
            fog: in _fog,
            skybox: in _skybox,
            dirLight: in _dirLight,
            shadows: in _shadow,
            postEffect: in _postEffect);

        _dirty = false;
        return _snapshot;
    }

    private static DirLightParams MakeDefaultDirLight() =>
        new(
            direction: new Vector3(-0.4f, -1.0f, 0.35f),
            diffuse: new Vector3(1.00f, 0.96f, 0.90f),
            intensity: 1.2f,
            specular: 0.7f
        );

    private static AmbientParams MakeDefaultAmbient() =>
        new(
            ambient: new(0.28f, 0.32f, 0.38f),
            ambientGround: new(0.18f, 0.16f, 0.14f),
            exposure: 0.2f
        );

    private static FogParams MakeDefaultFog() =>
        new(
            color: new(0.78f, 0.84f, 0.90f),
            density: 650f,
            heightFalloff: 6000f,
            baseHeight: 0f,
            strength: 1.0f,
            heightInfluence: 0.8f,
            scattering: 0.085f,
            maxDistance: 9000f
        );


    private static PostEffectParams MakeDefaultPostEffect() =>
        new(
            Grade: new(-0.10f, 0.80f, 0.805f, 0.70f),
            WhiteBalance: new(-0.10f, 0.25f),
            Bloom: new(0.35f, 0.75f, 8.0f),
            ImageFx: new(0.25f, 0.15f, 0.30f, 0.60f)
        );
}