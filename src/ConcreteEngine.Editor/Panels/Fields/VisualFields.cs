using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Editor.Lib;
using static ConcreteEngine.Editor.Controller.EngineObjects;

namespace ConcreteEngine.Editor.Panels.Fields;

internal static class LightPanelFields
{
    public static readonly FloatDragField<Float3Value> Direction = new("Direction", 0.01f, -1f, 1f,
        static () => Visuals.GetDirectionalLight().Direction,
        static value => Visuals.SetDirectionalLight(Visuals.GetDirectionalLight() with { Direction = (Vector3)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.High,
    };

    public static readonly ColorInputField Diffuse = new("Diffuse", false,
        static () => (Color4)Visuals.GetDirectionalLight().Diffuse,
        static value => Visuals.SetDirectionalLight(Visuals.GetDirectionalLight() with { Diffuse = value.ToVector3() }))
    {
        Delay = PropertyGetDelay.High
    };

    public static readonly FloatDragField<Float1Value> Intensity = new("Intensity", 0.01f, 0f, 10f,
        static () => Visuals.GetDirectionalLight().Intensity,
        static value => Visuals.SetDirectionalLight(Visuals.GetDirectionalLight() with { Intensity = (float)value }))
    {
        Format = "%.3f", Delay = PropertyGetDelay.High,
    };

    public static readonly FloatDragField<Float1Value> Specular = new("Specular", 0.01f, 0f, 10f,
        static () => Visuals.GetDirectionalLight().Specular,
        static value => Visuals.SetDirectionalLight(Visuals.GetDirectionalLight() with { Specular = (float)value }))
    {
        Format = "%.3f", Delay = PropertyGetDelay.High,
    };

    public static readonly ColorInputField Ambient = new("Ambient", false,
        static () => (Color4)Visuals.GetAmbient().Ambient,
        static value => Visuals.SetAmbient(Visuals.GetAmbient() with { Ambient = value.ToVector3() }))
    {
        Delay = PropertyGetDelay.High
    };

    public static readonly ColorInputField AmbientGround = new("Ambient Ground", false,
        static () => (Color4)Visuals.GetAmbient().AmbientGround,
        static value => Visuals.SetAmbient(Visuals.GetAmbient() with { AmbientGround = value.ToVector3() }))
    {
        Delay = PropertyGetDelay.High
    };

    public static readonly FloatDragField<Float1Value> Exposure = new("Exposure", 0.01f, 0f, 2f,
        static () => Visuals.GetAmbient().Exposure,
        static value => Visuals.SetAmbient(Visuals.GetAmbient() with { Exposure = (float)value }))
    {
        Format = "%.3f", Delay = PropertyGetDelay.High,
    };
}

internal static class FogPanelFields
{
    public static readonly ColorInputField FogColor = new("FogColor", false,
        static () => (Color4)Visuals.GetDirectionalLight().Diffuse,
        static value => Visuals.SetDirectionalLight(Visuals.GetDirectionalLight() with { Diffuse = value.ToVector3() }))
    {
        Delay = PropertyGetDelay.VeryHigh
    };

    public static readonly FloatDragField<Float1Value> Density = new("Density", 1f, 100, 1500,
        static () => Visuals.GetFog().Density,
        static value => Visuals.SetFog(Visuals.GetFog() with { Density = (float)value }))
    {
        Format = "%.5f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatDragField<Float1Value> BaseHeight = new("Base Height", 1f, -1000f, 1000f,
        static () => Visuals.GetFog().BaseHeight,
        static value => Visuals.SetFog(Visuals.GetFog() with { BaseHeight = (float)value }))
    {
        Format = "%.3f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatDragField<Float1Value> Falloff = new("Falloff", 1f, 0.001f, 10000.0f,
        static () => Visuals.GetFog().HeightFalloff,
        static value => Visuals.SetFog(Visuals.GetFog() with { HeightFalloff = (float)value }))
    {
        Format = "%.3f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatDragField<Float1Value> Influence = new("Influence", 0.001f, 0f, 1f,
        static () => Visuals.GetFog().HeightInfluence,
        static value => Visuals.SetFog(Visuals.GetFog() with { HeightInfluence = (float)value }))
    {
        Format = "%.3f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatDragField<Float1Value> Scattering = new("Scattering", 0.001f, 0.0f, 1.0f,
        static () => Visuals.GetFog().Scattering,
        static value => Visuals.SetFog(Visuals.GetFog() with { Scattering = (float)value }))
    {
        Format = "%.3f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatDragField<Float1Value> MaxDistance = new("Max Distance", 1f, 1f, 10000f,
        static () => Visuals.GetFog().MaxDistance,
        static value => Visuals.SetFog(Visuals.GetFog() with { MaxDistance = (float)value }))
    {
        Format = "%.0f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatDragField<Float1Value> Strength = new("Max Distance", 0.001f, 0f, 1f,
        static () => Visuals.GetFog().Strength,
        static value => Visuals.SetFog(Visuals.GetFog() with { Strength = (float)value }))
    {
        Format = "%.3f", Delay = PropertyGetDelay.VeryHigh,
    };
}

internal static class ShadowPanelFields
{
    public static readonly ComboField ShadowSizeCombo = new("Shadow Size", "No Shadow",
        [1024, 2048, 4096, 8192], ["1024px", "2048px", "4096px", "8192px"],
        static () => Visuals.GetShadow().ShadowMapSize,
        static value => Visuals.SetShadowSize(value)
    );

    public static readonly FloatSliderField<Float1Value> Distance = new("Distance", 10f, 200f,
        static () => Visuals.GetShadow().Distance,
        static value => Visuals.SetShadow(Visuals.GetShadow() with { Distance = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> ZPad = new("ZPad", 1f, 200f,
        static () => Visuals.GetShadow().ZPad,
        static value => Visuals.SetShadow(Visuals.GetShadow() with { ZPad = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> ConstBias = new("ConstBias", 0.0001f, 0.001f,
        static () => Visuals.GetShadow().ConstBias,
        static value => Visuals.SetShadow(Visuals.GetShadow() with { ConstBias = (float)value }))
    {
        Format = "%.5f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> SlopeBias = new("SlopeBias", 0.001f, 0.01f,
        static () => Visuals.GetShadow().SlopeBias,
        static value => Visuals.SetShadow(Visuals.GetShadow() with { SlopeBias = (float)value }))
    {
        Format = "%.4f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> Strength = new("Strength", 0f, 1f,
        static () => Visuals.GetShadow().Strength,
        static value => Visuals.SetShadow(Visuals.GetShadow() with { Strength = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> PcfRadius = new("PcfRadius", 0.5f, 4f,
        static () => Visuals.GetShadow().PcfRadius,
        static value => Visuals.SetShadow(Visuals.GetShadow() with { PcfRadius = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };
}

internal static class PostEffectPanelFields
{
    private static ref readonly PostGradeParams Grade => ref Visuals.GetPostEffect().Grade;

    public static readonly FloatSliderField<Float1Value> GradeExposure = new("Exposure", 0.5f, 2f,
        static () => Grade.Exposure,
        static value => Visuals.SetPostGrade(Grade with { Exposure = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> GradeSaturation = new("Saturation", 0f, 1.5f,
        static () => Grade.Saturation,
        static value => Visuals.SetPostGrade(Grade with { Saturation = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> GradeContrast = new("Contrast", 0f, 1.5f,
        static () => Grade.Contrast,
        static value => Visuals.SetPostGrade(Grade with { Contrast = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> GradeWarmth = new("Warmth", 0f, 1f,
        static () => Grade.Warmth,
        static value => Visuals.SetPostGrade(Grade with { Warmth = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> WbTint = new("Tint", 0f, 1f,
        static () => Visuals.GetPostEffect().WhiteBalance.Tint,
        static value => Visuals.SetPostWhiteBalance(Visuals.GetPostEffect().WhiteBalance with { Tint = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> WbStrength = new("Strength", -1f, 1f,
        static () => Visuals.GetPostEffect().WhiteBalance.Strength,
        static value =>
            Visuals.SetPostWhiteBalance(Visuals.GetPostEffect().WhiteBalance with { Strength = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };

    private static ref readonly PostBloomParams Bloom => ref Visuals.GetPostEffect().Bloom;


    public static readonly FloatSliderField<Float1Value> BloomIntensity = new("Intensity", 0f, 2f,
        static () => Bloom.Intensity,
        static value => Visuals.SetPostBloom(Bloom with { Intensity = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> BloomThreshold = new("Threshold", 0f, 2f,
        static () => Bloom.Threshold,
        static value => Visuals.SetPostBloom(Bloom with { Threshold = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> BloomRadius = new("Radius", 0f, 10f,
        static () => Bloom.Radius,
        static value => Visuals.SetPostBloom(Bloom with { Radius = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };

    private static ref readonly PostImageFxParams ImageFx => ref Visuals.GetPostEffect().ImageFx;

    public static readonly FloatSliderField<Float1Value> FxVignette = new("Vignette", 0f, 0.5f,
        static () => ImageFx.Vignette,
        static value => Visuals.SetPostImageFx(ImageFx with { Vignette = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> FxGrain = new("Grain", 0f, 0.5f,
        static () => ImageFx.Grain,
        static value => Visuals.SetPostImageFx(ImageFx with { Grain = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> FxSharpen = new("xSharpen", 0f, 0.5f,
        static () => ImageFx.Sharpen,
        static value => Visuals.SetPostImageFx(ImageFx with { Sharpen = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> FxRolloff = new("Rolloff", 0f, 0.5f,
        static () => ImageFx.Rolloff,
        static value => Visuals.SetPostImageFx(ImageFx with { Rolloff = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh,
    };
}