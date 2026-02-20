using ConcreteEngine.Core.Renderer.Visuals;

namespace ConcreteEngine.Core.Engine;

public abstract class VisualEnvironment
{
    protected AmbientParams Ambient;
    protected FogParams Fog;
    protected SunLightParams SunLight;
    protected ShadowParams Shadow;
    protected PostEffectParams PostEffect;

    public ref readonly AmbientParams GetAmbient() => ref Ambient;
    public ref readonly SunLightParams GetDirectionalLight() => ref SunLight;
    public ref readonly ShadowParams GetShadow() => ref Shadow;
    public ref readonly FogParams GetFog() => ref Fog;
    public ref readonly PostEffectParams GetPostEffect() => ref PostEffect;

    public abstract void SetDirectionalLight(in SunLightParams param);
    public abstract void SetAmbient(in AmbientParams param);
    public abstract void SetFog(in FogParams param);
    public abstract void SetPostEffect(in PostEffectParams param);
    public abstract void SetPostGrade(in PostGradeParams param);
    public abstract void SetPostWhiteBalance(in PostWhiteBalanceParams param);
    public abstract void SetPostBloom(in PostBloomParams param);
    public abstract void SetPostImageFx(in PostImageFxParams param);

    public abstract void SetShadow(in ShadowParams param);
    public abstract bool SetShadowSize(int size);
}