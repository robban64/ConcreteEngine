using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Specs.Visuals;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldVisual
{
    private bool _dirty = true;
    private bool _clearSnapshotDirtyNext;

    private readonly RenderParamsSnapshot _snapshot = new();

    private AmbientParams _ambient;
    private FogParams _fog;
    private SunLightParams _sunLight;
    private ShadowParams _shadow;
    private PostEffectParams _postEffect;

    internal WorldVisual()
    {
        _shadow = WorldParamUtils.MakeSizedShadow(EngineSettings.Instance.Graphics.ShadowSize);
        _ambient = WorldParamUtils.MakeDefaultAmbient();
        _fog = WorldParamUtils.MakeDefaultFog();
        _sunLight = WorldParamUtils.MakeDefaultSunLight();
        _postEffect = WorldParamUtils.MakeDefaultPostEffect();
        EndTick();
    }

    public long Generation { get; private set; }

    internal RenderParamsSnapshot Snapshot => _snapshot;

    internal int ShadowMapSize => _shadow.ShadowMapSize;

    public void SetDirectionalLight(in SunLightParams param)
    {
        _sunLight = param;
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

    public bool SetShadow(int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, RenderLimits.MinShadowMapSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, RenderLimits.MaxShadowMapSize);
        ArgumentOutOfRangeException.ThrowIfZero(IntMath.IsPowerOfTwo(size) ? 1 : 0);
        if (size == ShadowMapSize) return false;

        _shadow = WorldParamUtils.MakeSizedShadow(size);
        _dirty = true;
        return true;
    }

    internal void SetFromData(in WorldParamsData data)
    {
        int size = _shadow.ShadowMapSize;
        _shadow = data.Shadow;
        _shadow.ShadowMapSize = size;

        _ambient = data.Ambient;
        _sunLight = data.SunLight;
        _fog = data.Fog;
        _postEffect = data.PostEffect;
        _dirty = true;
    }

    internal void FillData(out WorldParamsData data)
    {
        data.SunLight = _sunLight;
        data.Ambient = _ambient;
        data.Fog = _fog;
        data.PostEffect = _postEffect;
        data.Shadow = _shadow;
    }

    internal void EndTick()
    {
        if (!_dirty)
        {
            if (_snapshot.IsDirty && !_clearSnapshotDirtyNext) _clearSnapshotDirtyNext = true;
            else if (_snapshot.IsDirty && _clearSnapshotDirtyNext)
            {
                _snapshot.IsDirty = false;
                _clearSnapshotDirtyNext = false;
            }

            return;
        }

        Generation++;

        _snapshot.Ambient = _ambient;
        _snapshot.Fog = _fog;
        _snapshot.SunLight = _sunLight;
        _snapshot.Shadows = _shadow;
        _snapshot.PostEffect = _postEffect;


        _dirty = false;
        _snapshot.IsDirty = true;
    }
}