#region

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.State;
using ConcreteEngine.Shared.RenderData;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

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

    public void SetDirectionalLight(in DirLightParams param)
    {
        _dirLight = param;
        _dirty = true;
    }

    public void SetAmbient(in AmbientParams param)
    {
        _ambient = param;
        _dirty = true;
    }

    public void SetFog(in FogParams param)
    {
        _fog = param;
        _dirty = true;
    }


    public void SetPostEffect(in PostEffectParams param)
    {
        _postEffect = param;
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

    internal void FromEditor(in WorldParamsData data)
    {
        _ambient = data.Ambient;
        _dirLight = data.DirLight;
        _fog = data.Fog;
        _postEffect = data.PostEffect;
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

    private static DirLightParams MakeDefaultDirLight() =>
        new(
            direction: new Vector3(-0.35f, -0.95f, 0.25f),
            diffuse: new Vector3(1.05f, 0.92f, 0.82f),
            intensity: 1.35f,
            specular: 0.75f
        );

    private static AmbientParams MakeDefaultAmbient() =>
        new(
            ambient: new Vector3(0.34f, 0.38f, 0.44f),
            ambientGround: new Vector3(0.20f, 0.17f, 0.15f),
            exposure: 0.26f
        );

    private static FogParams MakeDefaultFog() =>
        new(
            Color: new Vector3(0.70f, 0.89f, 0.68f),
            Density: 720f,
            HeightFalloff: 5200f,
            BaseHeight: 0f,
            Strength: 1.05f,
            HeightInfluence: 0.85f,
            Scattering: 0.09f,
            MaxDistance: 9500f
        );


    private static PostEffectParams MakeDefaultPostEffect() =>
        new(
            grade: new PostGradeParams(-0.10f, 0.80f, 0.805f, 0.70f),
            whiteBalance: new PostWhiteBalanceParams(-0.10f, 0.25f),
            bloom: new PostBloomParams(0.35f, 0.75f, 8.0f),
            imageFx: new PostImageFxParams(0.25f, 0.15f, 0.30f, 0.60f)
        );
}