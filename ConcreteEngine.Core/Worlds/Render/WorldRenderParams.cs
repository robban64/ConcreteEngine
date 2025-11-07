#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.State;

#endregion

namespace ConcreteEngine.Core.Worlds.Render;

public sealed class WorldRenderParams
{
    private bool _dirty = true;
    private bool _clearSnapshotDirtyNext = false;
    private readonly RenderParamsSnapshot _snapshot = new();

    private AmbientParams _ambient = MakeDefaultAmbient();
    private FogParams _fog = MakeDefaultFog();
    private DirLightParams _dirLight = MakeDefaultDirLight();
    private ShadowParams _shadow;
    private PostEffectParams _postEffect = MakeDefaultPostEffect();

    public long Version { get; private set; } = 0;

    internal RenderParamsSnapshot Snapshot => _snapshot;

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

    public void SetShadowDefault(int size, float? distance = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, RenderLimits.MinShadowMapSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, RenderLimits.MaxShadowMapSize);

        if (distance is not { } dist)
            dist = _shadow.Distance > 1 ? _shadow.Distance : 120f;

        var constBias = 0.8f / size;
        var slopeBias = constBias * 6f;
        //var constBias = 1.2f / size;
        //var slopeBias = constBias * 3f;
        _shadow = new ShadowParams(
            shadowMapSize: size,
            distance: dist,
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

    internal RenderParamsSnapshot Commit()
    {
        if (!_dirty)
        {
            if (_snapshot.IsDirty && !_clearSnapshotDirtyNext) _clearSnapshotDirtyNext = true;
            else if (_snapshot.IsDirty && _clearSnapshotDirtyNext)
            {
                _snapshot.ClearDirty();
                _clearSnapshotDirtyNext = false;
            }

            return _snapshot;
        }

        Version++;

        _snapshot.Update(
            version: Version,
            ambient: in _ambient,
            fog: in _fog,
            dirLight: in _dirLight,
            shadows: in _shadow,
            postEffect: in _postEffect);

        _dirty = false;
        return _snapshot;
    }

    private static DirLightParams MakeDefaultDirLight() => new(
        direction: new Vector3(-0.35f, -0.95f, 0.25f),
        diffuse: new Vector3(1.05f, 0.92f, 0.82f),
        intensity: 1.35f,
        specular: 0.75f
    );

    private static AmbientParams MakeDefaultAmbient() => new(
        ambient: new Vector3(0.34f, 0.38f, 0.44f),
        ambientGround: new Vector3(0.20f, 0.17f, 0.15f),
        exposure: 0.26f
    );

    private static FogParams MakeDefaultFog() => new(
        color: new Vector3(0.70f, 0.89f, 0.68f),
        density: 720f,
        heightFalloff: 5200f,
        baseHeight: 0f,
        strength: 1.05f,
        heightInfluence: 0.85f,
        scattering: 0.09f,
        maxDistance: 9500f
    );


    private static PostEffectParams MakeDefaultPostEffect() => new(
        grade: new(-0.10f, 0.80f, 0.805f, 0.70f),
        whiteBalance: new(-0.10f, 0.25f),
        bloom: new(0.35f, 0.75f, 8.0f),
        imageFx: new(0.25f, 0.15f, 0.30f, 0.60f)
    );
}