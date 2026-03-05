using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Worlds;
/*
public sealed class WorldVisual : VisualEnvironment
{
    private bool _dirty = true;

    private readonly RenderParamsSnapshot _snapshot;

    public long Generation { get; private set; }

    internal WorldVisual(RenderParamsSnapshot snapshot, Size2D outputSize)
    {
        _snapshot = snapshot;
        _snapshot.ScreenFboSize = outputSize;
        Shadow = WorldParamUtils.MakeSizedShadow(EngineSettings.Instance.Graphics.ShadowSize);
        Ambient = WorldParamUtils.MakeDefaultAmbient();
        Fog = WorldParamUtils.MakeDefaultFog();
        SunLight = WorldParamUtils.MakeDefaultSunLight();
        PostEffect = WorldParamUtils.MakeDefaultPostEffect();
        EndTick();
    }

    internal int ShadowMapSize => _snapshot.Shadow.ShadowMapSize;

    internal void SetScreenFboSize(Size2D outputSize)
    {
        _snapshot.ScreenFboSize = outputSize;
        _dirty = true;
    }

    public override void SetDirectionalLight(in SunLightParams param)
    {
        SunLight = param;
        _dirty = true;
    }

    public override void SetAmbient(in AmbientParams param)
    {
        Ambient = param;
        _dirty = true;
    }

    public override void SetFog(in FogParams param)
    {
        Fog = param;
        _dirty = true;
    }

    public override void SetPostEffect(in PostEffectParams param)
    {
        PostEffect = param;
        _dirty = true;
    }

    public override void SetPostGrade(in PostGradeParams param)
    {
        PostEffect.Grade = param;
        _dirty = true;
    }

    public override void SetPostWhiteBalance(in PostWhiteBalanceParams param)
    {
        PostEffect.WhiteBalance = param;
        _dirty = true;
    }

    public override void SetPostBloom(in PostBloomParams param)
    {
        PostEffect.Bloom = param;
        _dirty = true;
    }

    public override void SetPostImageFx(in PostImageFxParams param)
    {
        PostEffect.ImageFx = param;
        _dirty = true;
    }

    public override void SetShadow(in ShadowParams param)
    {
        Shadow = param;
        _dirty = true;
    }

    public override bool SetShadowSize(int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, RenderLimits.MinShadowMapSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, RenderLimits.MaxShadowMapSize);
        ArgumentOutOfRangeException.ThrowIfZero(IntMath.IsPowerOfTwo(size) ? 1 : 0);
        if (size == ShadowMapSize) return false;

        Shadow = WorldParamUtils.MakeSizedShadow(size);
        _dirty = true;
        return true;
    }

    private void FillSnapshot()
    {
        _snapshot.SunLight = SunLight;
        _snapshot.Ambient = Ambient;
        _snapshot.Fog = Fog;
        _snapshot.PostEffect = PostEffect;
        _snapshot.Shadow = Shadow;
    }

    internal void EndTick()
    {
        if (_dirty && !_snapshot.IsDirty)
        {
            _dirty = false;
            _snapshot.IsDirty = true;
            FillSnapshot();
            Generation++;
        }
    }
}*/