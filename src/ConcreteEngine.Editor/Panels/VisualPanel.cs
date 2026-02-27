using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Bridge.EngineObjects;

namespace ConcreteEngine.Editor.Panels;

internal sealed class VisualPanel(StateContext context) : EditorPanel(PanelId.Visual, context)
{
    private AvgFrameTimer _avgFrameTimer;

    public override void Enter()
    {
        _gradeFields.Refresh();
        _wbFields.Refresh();
        _bloomFields.Refresh();
        _imageFxFields.Refresh();
    }

    public override void Draw(FrameContext ctx)
    {
        _avgFrameTimer.BeginSample();
        ImGui.BeginGroup();
        ImGui.SeparatorText("Grade"u8);
        _gradeFields.Draw();
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        ImGui.SeparatorText("White Balance"u8);
        _wbFields.Draw();
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        ImGui.SeparatorText("Bloom"u8);
        _bloomFields.Draw();
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        ImGui.SeparatorText("Image FX"u8);
        _imageFxFields.Draw();
        ImGui.EndGroup();

        _avgFrameTimer.EndSample();
        if (_avgFrameTimer.Count > 40) _avgFrameTimer.ResetAndPrint();
    }
    
    
    private readonly FloatGroupField<Float4Value> _gradeFields = new FloatGroupField<Float4Value>("Grade",
            static () =>
            {
                ref readonly var it = ref Visuals.GetPostEffect().Grade;
                return new Float4Value(it.Exposure, it.Saturation, it.Contrast, it.Warmth);
            },
            static value => Visuals.SetPostGrade(new PostGradeParams(value.X, value.Y, value.Z, value.W))
        )
        .WithDelay(PropertyGetDelay.VeryHigh)
        .WithSlider("Exposure", 0.5f, 2f)
        .WithSlider("Saturation", 0f, 1.5f)
        .WithSlider("Contrast", 0f, 1.5f)
        .WithSlider("Warmth", 0f, 1f);

    private readonly FloatGroupField<Float4Value> _imageFxFields = new FloatGroupField<Float4Value>("Image Fx",
            static () =>
            {
                ref readonly var it = ref Visuals.GetPostEffect().ImageFx;
                return new Float4Value(it.Vignette, it.Grain, it.Sharpen, it.Rolloff);
            },
            static value => Visuals.SetPostImageFx(new PostImageFxParams(value.X, value.Y, value.Z, value.W))
        )
        .WithDelay(PropertyGetDelay.VeryHigh)
        .WithSlider("Vignette", 0f, 0.5f)
        .WithSlider("Grain", 0f, 0.5f)
        .WithSlider("Sharpen", 0f, 0.5f)
        .WithSlider("Rolloff", 0f, 0.5f);

    private readonly FloatGroupField<Float3Value> _bloomFields = new FloatGroupField<Float3Value>("Bloom",
            static () =>
            {
                ref readonly var it = ref Visuals.GetPostEffect().Bloom;
                return new Float3Value(it.Intensity, it.Threshold, it.Radius);
            },
            static value => Visuals.SetPostBloom(new PostBloomParams(value.X, value.Y, value.Z))
        )
        .WithDelay(PropertyGetDelay.VeryHigh)
        .WithSlider("Intensity", 0f, 2f)
        .WithSlider("Threshold", 0f, 2f)
        .WithSlider("Radius", 0f, 10f);


    private readonly FloatGroupField<Float2Value> _wbFields = new FloatGroupField<Float2Value>("White Balance",
            static () =>
            {
                var it = Visuals.GetPostEffect().WhiteBalance;
                return new Float2Value(it.Tint, it.Strength);
            },
            static value => Visuals.SetPostWhiteBalance(new PostWhiteBalanceParams(value.X, value.Y))
        )
        .WithDelay(PropertyGetDelay.VeryHigh)
        .WithSlider("Tint", 0f, 1f)
        .WithSlider("Strength", -1f, 1f);
}
/*
file static class PostEffectPanelFields
{
    private static ref readonly PostGradeParams Grade => ref Visuals.GetPostEffect().Grade;

    public static readonly FloatField<Float1Value> GradeExposure = new("Exposure", FieldWidgetKind.Slider,
        static () =>  Visuals.GetPostEffect().Grade.Exposure,
        static value => Visuals.SetPostGrade(Grade with { Exposure = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0.5f,
        Max = 2f
    };

    public static readonly FloatField<Float1Value> GradeSaturation = new("Saturation", FieldWidgetKind.Slider,
        static () => Visuals.GetPostEffect().Grade.Saturation,
        static value => Visuals.SetPostGrade(Grade with { Saturation = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0f,
        Max = 1.5f
    };

    public static readonly FloatField<Float1Value> GradeContrast = new("Contrast", FieldWidgetKind.Slider,
        static () => Visuals.GetPostEffect().Grade.Contrast,
        static value => Visuals.SetPostGrade(Grade with { Contrast = (float)value }))
    {
        Layout = FieldLabelLayout.Inline,
        Format = "%.2f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0,
        Max = 1.5f
    };

    public static readonly FloatField<Float1Value> GradeWarmth = new("Warmth", FieldWidgetKind.Slider,
        static () => Visuals.GetPostEffect().Grade.Warmth,
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