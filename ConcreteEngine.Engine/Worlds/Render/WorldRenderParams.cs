#region

using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Utility;
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

    private AmbientParams _ambient = WorldParamUtils.MakeDefaultAmbient();
    private FogParams _fog = WorldParamUtils.MakeDefaultFog();
    private SunLightParams _sunLight = WorldParamUtils.MakeDefaultSunLight();
    private ShadowParams _shadow = WorldParamUtils.MakeSizedShadow(4096);
    private PostEffectParams _postEffect = WorldParamUtils.MakeDefaultPostEffect();

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

    public void SetShadow(int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, RenderLimits.MinShadowMapSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, RenderLimits.MaxShadowMapSize);
        ArgumentOutOfRangeException.ThrowIfZero(IntMath.IsPowerOfTwo(size) ? 1 : 0);

        _shadow = WorldParamUtils.MakeSizedShadow(size);
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

    internal RenderParamsSnapshot EndTick()
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

        Generation++;
        
        _snapshot.Ambient = _ambient;
        _snapshot.Fog = _fog;
        _snapshot.SunLight = _sunLight;
        _snapshot.Shadows = _shadow;
        _snapshot.PostEffect = _postEffect;


        _dirty = false;
        return _snapshot;
    }
}