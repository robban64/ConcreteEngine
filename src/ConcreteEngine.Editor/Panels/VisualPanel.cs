using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Bridge.EngineObjects;

namespace ConcreteEngine.Editor.Panels;

internal sealed class VisualPanel(StateContext context) : EditorPanel(PanelId.Visual, context)
{
    public override void Draw(FrameContext ctx)
    {
        ImGui.BeginGroup();
        ImGui.SeparatorText("Grade"u8);
        PostEffectPanelFields.GradeExposure.DrawField(false);
        PostEffectPanelFields.GradeSaturation.DrawField(false);
        PostEffectPanelFields.GradeContrast.DrawField(false);
        PostEffectPanelFields.GradeWarmth.DrawField(false);
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        ImGui.SeparatorText("White Balance"u8);
        PostEffectPanelFields.WbTint.DrawField(false);
        PostEffectPanelFields.WbStrength.DrawField(false);
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        ImGui.SeparatorText("Bloom"u8);
        PostEffectPanelFields.BloomIntensity.DrawField(false);
        PostEffectPanelFields.BloomThreshold.DrawField(false);
        PostEffectPanelFields.BloomRadius.DrawField(false);
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        ImGui.SeparatorText("Image FX"u8);
        PostEffectPanelFields.FxVignette.DrawField(false);
        PostEffectPanelFields.FxGrain.DrawField(false);
        PostEffectPanelFields.FxSharpen.DrawField(false);
        PostEffectPanelFields.FxRolloff.DrawField(false);
        ImGui.EndGroup();
    }
}

file static class PostEffectPanelFields
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