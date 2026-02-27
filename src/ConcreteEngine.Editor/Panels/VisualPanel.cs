using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Bridge.EngineObjects;

namespace ConcreteEngine.Editor.Panels;

internal sealed class VisualPanel : EditorPanel
{
    private readonly FloatGroupField<Float4Value> _grade = new("grade", FieldWidgetKind.Slider,
        static () =>
        {
            var g = Visuals.GetPostEffect().Grade;
            return new Float4Value(g.Exposure, g.Saturation, g.Contrast, g.Warmth);
        },
        static value => Visuals.SetPostGrade(new PostGradeParams(value.X, value.Y, value.Z, value.W))
    ) { Delay = PropertyGetDelay.VeryHigh };

    private readonly FloatGroupField<Float4Value> _imageFx = new("imageFx", FieldWidgetKind.Slider,
        static () =>
        {
            var it = Visuals.GetPostEffect().ImageFx;
            return new Float4Value(it.Vignette, it.Grain, it.Sharpen, it.Rolloff);
        },
        static value => Visuals.SetPostImageFx(new PostImageFxParams(value.X, value.Y, value.Z, value.W))
    ) { Delay = PropertyGetDelay.VeryHigh };

    private readonly FloatGroupField<Float4Value> _bloom = new("bloom", FieldWidgetKind.Slider,
        static () =>
        {
            var it = Visuals.GetPostEffect().Bloom;
            return new Float4Value(it.Intensity, it.Threshold, it.Radius, 0);
        },
        static value => Visuals.SetPostBloom(new PostBloomParams(value.X, value.Y, value.Z))
    ) { Delay = PropertyGetDelay.VeryHigh };

    public readonly FloatField<Float1Value> WbTint = new("Tint", FieldWidgetKind.Slider,
        static () => Visuals.GetPostEffect().WhiteBalance.Tint,
        static value => Visuals.SetPostWhiteBalance(Visuals.GetPostEffect().WhiteBalance with { Tint = (float)value }))
    {
        Layout = FieldLabelLayout.Inline, Delay = PropertyGetDelay.VeryHigh, Min = 0, Max = 1f
    };

    public readonly FloatField<Float1Value> WbStrength = new("Strength", FieldWidgetKind.Slider,
        static () => Visuals.GetPostEffect().WhiteBalance.Strength,
        static value =>
            Visuals.SetPostWhiteBalance(Visuals.GetPostEffect().WhiteBalance with { Strength = (float)value }))
    {
        Layout = FieldLabelLayout.Inline, Delay = PropertyGetDelay.VeryHigh, Min = -1f, Max = 1f
    };


    public VisualPanel(StateContext context) : base(PanelId.Visual, context)
    {
        _grade.AddField(new FloatGroupEntry("Exposure", 0.5f, 2f));
        _grade.AddField(new FloatGroupEntry("Saturation", 0f, 1.5f));
        _grade.AddField(new FloatGroupEntry("Contrast", 0f, 1.5f));
        _grade.AddField(new FloatGroupEntry("Warmth", 0f, 1f));

        _imageFx.AddField(new FloatGroupEntry("Vignette", 0f, 0.5f));
        _imageFx.AddField(new FloatGroupEntry("Grain", 0f, 0.5f));
        _imageFx.AddField(new FloatGroupEntry("Sharpen", 0f, 0.5f));
        _imageFx.AddField(new FloatGroupEntry("Rolloff", 0f, 0.5f));

        _bloom.AddField(new FloatGroupEntry("Intensity", 0f, 2f));
        _bloom.AddField(new FloatGroupEntry("Threshold", 0f, 2f));
        _bloom.AddField(new FloatGroupEntry("Radius", 0f, 10f));
    }


    public override void Draw(FrameContext ctx)
    {
        ImGui.BeginGroup();
        ImGui.SeparatorText("Grade"u8);
        _grade.Draw();
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        ImGui.SeparatorText("White Balance"u8);
        WbTint.Draw();
        WbStrength.Draw();
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        ImGui.SeparatorText("Bloom"u8);
        _bloom.Draw();
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        ImGui.SeparatorText("Image FX"u8);
        _imageFx.Draw();
        ImGui.EndGroup();
    }
}
/*
file static class PostEffectPanelFields
{
    private static ref readonly PostGradeParams Grade => ref Visuals.GetPostEffect().Grade;

    public static readonly FloatField<Float1Value> GradeExposure = new("Exposure", FieldWidgetKind.Slider,
        static () => Grade.Exposure,
        static value => Visuals.SetPostGrade(Grade with { Exposure = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0.5f,
        Max = 2f
    };

    public static readonly FloatField<Float1Value> GradeSaturation = new("Saturation", FieldWidgetKind.Slider,
        static () => Grade.Saturation,
        static value => Visuals.SetPostGrade(Grade with { Saturation = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0f,
        Max = 1.5f
    };

    public static readonly FloatField<Float1Value> GradeContrast = new("Contrast", FieldWidgetKind.Slider,
        static () => Grade.Contrast,
        static value => Visuals.SetPostGrade(Grade with { Contrast = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0,
        Max = 1.5f
    };

    public static readonly FloatField<Float1Value> GradeWarmth = new("Warmth", FieldWidgetKind.Slider,
        static () => Grade.Warmth,
        static value => Visuals.SetPostGrade(Grade with { Warmth = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0,
        Max = 1f
    };

    public static readonly FloatField<Float1Value> WbTint = new("Tint", FieldWidgetKind.Slider,
        static () => Visuals.GetPostEffect().WhiteBalance.Tint,
        static value => Visuals.SetPostWhiteBalance(Visuals.GetPostEffect().WhiteBalance with { Tint = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0,
        Max = 1f
    };

    public static readonly FloatField<Float1Value> WbStrength = new("Strength", FieldWidgetKind.Slider,
        static () => Visuals.GetPostEffect().WhiteBalance.Strength,
        static value =>
            Visuals.SetPostWhiteBalance(Visuals.GetPostEffect().WhiteBalance with { Strength = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = -1f,
        Max = 1f
    };

    private static ref readonly PostBloomParams Bloom => ref Visuals.GetPostEffect().Bloom;


    public static readonly FloatField<Float1Value> BloomIntensity = new("Intensity", FieldWidgetKind.Slider,
        static () => Bloom.Intensity,
        static value => Visuals.SetPostBloom(Bloom with { Intensity = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0,
        Max = 2f
    };

    public static readonly FloatField<Float1Value> BloomThreshold = new("Threshold", FieldWidgetKind.Slider,
        static () => Bloom.Threshold,
        static value => Visuals.SetPostBloom(Bloom with { Threshold = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0,
        Max = 2f
    };

    public static readonly FloatField<Float1Value> BloomRadius = new("Radius", FieldWidgetKind.Slider,
        static () => Bloom.Radius,
        static value => Visuals.SetPostBloom(Bloom with { Radius = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0,
        Max = 10f
    };

    private static ref readonly PostImageFxParams ImageFx => ref Visuals.GetPostEffect().ImageFx;

    public static readonly FloatField<Float1Value> FxVignette = new("Vignette", FieldWidgetKind.Slider,
        static () => ImageFx.Vignette,
        static value => Visuals.SetPostImageFx(ImageFx with { Vignette = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0,
        Max = 0.5f
    };

    public static readonly FloatField<Float1Value> FxGrain = new("Grain", FieldWidgetKind.Slider,
        static () => ImageFx.Grain,
        static value => Visuals.SetPostImageFx(ImageFx with { Grain = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0,
        Max = 0.5f
    };

    public static readonly FloatField<Float1Value> FxSharpen = new("xSharpen", FieldWidgetKind.Slider,
        static () => ImageFx.Sharpen,
        static value => Visuals.SetPostImageFx(ImageFx with { Sharpen = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0,
        Max = 0.5f
    };

    public static readonly FloatField<Float1Value> FxRolloff = new("Rolloff", FieldWidgetKind.Slider,
        static () => ImageFx.Rolloff,
        static value => Visuals.SetPostImageFx(ImageFx with { Rolloff = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0,
        Max = 0.5f
    };
}*/