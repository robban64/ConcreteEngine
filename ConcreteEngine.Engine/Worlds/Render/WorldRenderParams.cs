#region

using System.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.State;
using ConcreteEngine.Shared.Rendering;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

public sealed class WorldRenderParams
{
    private bool _dirty = true;
    private bool _clearSnapshotDirtyNext;
    
    private readonly RenderParamsSnapshot _snapshot = new();

    private AmbientParams _ambient = MakeDefaultAmbient();
    private FogParams _fog = MakeDefaultFog();
    private DirLightParams _dirLight = MakeDefaultDirLight();
    private ShadowParams _shadow = MakeDefaultShadow(4096);
    private PostEffectParams _postEffect = MakeDefaultPostEffect();

    public long Version { get; private set; }
    public bool PendingShadowSize { get; private set; } = true;

    internal RenderParamsSnapshot Snapshot => _snapshot;

    internal int ShadowMapSize => _shadow.ShadowMapSize;

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

    public void SetShadow(in ShadowParams param)
    {
        var shadowSize = param.ShadowMapSize;
        ArgumentOutOfRangeException.ThrowIfLessThan(shadowSize, RenderLimits.MinShadowMapSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(shadowSize, RenderLimits.MaxShadowMapSize);
        ArgumentOutOfRangeException.ThrowIfZero(IntMath.IsPowerOfTwo(shadowSize) ? 1 : 0);
        
        if (_shadow.ShadowMapSize != param.ShadowMapSize)
            PendingShadowSize = true;
        
        _shadow = param;
    }

    internal void SetFromData(in WorldParamsData data)
    {
        SetShadow(in data.Shadow);
        _ambient = data.Ambient;
        _dirLight = data.DirLight;
        _fog = data.Fog;
        _postEffect = data.PostEffect;
        _dirty = true;
    }

    internal void FillData(out WorldParamsData data)
    {
        data.DirLight = _dirLight;
        data.Ambient = _ambient;
        data.Fog = _fog;
        data.PostEffect = _postEffect;
        data.Shadow = _shadow;
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

    internal void ClearPending() => PendingShadowSize = false;

    private static ShadowParams MakeDefaultShadow(int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, RenderLimits.MinShadowMapSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, RenderLimits.MaxShadowMapSize);


        //constBias
        // 4k-map =  0.0003 to 0.0005
        // 2k-map = 0.0001 to 0.0002
        
        //slopeBias
        // 2k-map = 0.0025 - 0.0035
        // 4k-map = 0.0015f-0.0025f

        (int distance, float constBias, float slopeBias) = size switch
        {
            2048 => (80, 0.0002f, 0.003f),
            4096 => (120, 0.0004f, 0.002f),
            _ => (80, 0.0002f, 0.003f)
        };
        return new ShadowParams(
            shadowMapSize: size,
            distance: distance,
            zPad: 20.0f,
            constBias: constBias,
            slopeBias: slopeBias,
            strength: 1f,
            pcfRadius: 1f);
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
            color: new Vector3(0.70f, 0.89f, 0.68f),
            density: 720f,
            heightFalloff: 5200f,
            baseHeight: 0f,
            strength: 1.05f,
            heightInfluence: 0.85f,
            scattering: 0.09f,
            maxDistance: 9500f
        );


    private static PostEffectParams MakeDefaultPostEffect() =>
        new(
            grade: new PostGradeParams(-0.10f, 0.80f, 0.805f, 0.70f),
            whiteBalance: new PostWhiteBalanceParams(-0.10f, 0.25f),
            bloom: new PostBloomParams(0.35f, 0.75f, 8.0f),
            imageFx: new PostImageFxParams(0.25f, 0.15f, 0.30f, 0.60f)
        );
}