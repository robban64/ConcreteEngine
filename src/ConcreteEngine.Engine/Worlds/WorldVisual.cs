using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldVisual
{
    private bool _dirty = true;

    private readonly RenderParamsSnapshot _snapshot;

    public ref readonly AmbientParams Ambient =>ref _snapshot.Ambient;
    public ref readonly FogParams Fog =>ref _snapshot.Fog;
    public ref readonly SunLightParams SunLight =>ref _snapshot.SunLight;
    public ref readonly ShadowParams Shadow =>ref _snapshot.Shadow;
    public ref readonly PostEffectParams PostEffect => ref _snapshot.PostEffect;


    internal WorldVisual(RenderParamsSnapshot snapshot, Size2D outputSize)
    {
        _snapshot = snapshot;
        _snapshot.ScreenFboSize = outputSize;
        _snapshot.Shadow = WorldParamUtils.MakeSizedShadow(EngineSettings.Instance.Graphics.ShadowSize);
        _snapshot.Ambient = WorldParamUtils.MakeDefaultAmbient();
        _snapshot.Fog = WorldParamUtils.MakeDefaultFog();
        _snapshot.SunLight = WorldParamUtils.MakeDefaultSunLight();
        _snapshot.PostEffect = WorldParamUtils.MakeDefaultPostEffect();
        EndTick();
    }

    public long Generation { get; private set; }

    internal RenderParamsSnapshot Snapshot => _snapshot;

    internal int ShadowMapSize => _snapshot.Shadow.ShadowMapSize;
    
    internal void SetScreenFboSize(Size2D outputSize)
    {
        _snapshot.ScreenFboSize = outputSize;
        _dirty = true;
    }

    public void SetDirectionalLight(in SunLightParams param)
    {
        _snapshot.SunLight = param;
        _dirty = true;
    }

    public void SetAmbient(in AmbientParams param)
    {
        _snapshot.Ambient = param;
        _dirty = true;
    }

    public void SetFog(in FogParams param)
    {
        _snapshot.Fog = param;
        _dirty = true;
    }


    public void SetPostEffect(in PostEffectParams param)
    {
        _snapshot.PostEffect = param;
        _dirty = true;
    }

    public bool SetShadow(int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, RenderLimits.MinShadowMapSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, RenderLimits.MaxShadowMapSize);
        ArgumentOutOfRangeException.ThrowIfZero(IntMath.IsPowerOfTwo(size) ? 1 : 0);
        if (size == ShadowMapSize) return false;

        _snapshot.Shadow = WorldParamUtils.MakeSizedShadow(size);
        _dirty = true;
        return true;
    }

    internal void SetFromData(in EditorVisualState data)
    {
        var sn = _snapshot;
        int size = sn.Shadow.ShadowMapSize;
        sn.Shadow = data.Shadow;
        sn.Shadow.ShadowMapSize = size;

        sn.Ambient = data.Ambient;
        sn.SunLight = data.SunLight;
        sn.Fog = data.Fog;
        sn.PostEffect = data.PostEffect;
        _dirty = true;
    }

    internal void FillData(out EditorVisualState data)
    {
        var sn = _snapshot;
        data.SunLight = sn.SunLight;
        data.Ambient = sn.Ambient;
        data.Fog = sn.Fog;
        data.PostEffect = sn.PostEffect;
        data.Shadow = sn.Shadow;
    }

    internal void EndTick()
    {
        if (_dirty && !_snapshot.IsDirty)
        {
            _dirty = false;
            _snapshot.IsDirty = true;
            Generation++;
        }
    }
}