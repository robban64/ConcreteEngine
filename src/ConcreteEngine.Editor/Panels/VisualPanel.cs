using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Bridge.EngineObjects;

namespace ConcreteEngine.Editor.Panels;

internal sealed class VisualPanel(StateContext context) : EditorPanel(PanelId.Visual, context)
{
    public override void Enter()
    {
        _gradeFields.Refresh();
        _wbFields.Refresh();
        _bloomFields.Refresh();
        _imageFxFields.Refresh();
    }

    public override void Draw(FrameContext ctx)
    {
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
