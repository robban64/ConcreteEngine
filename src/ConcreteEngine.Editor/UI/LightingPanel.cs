using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Bridge.EngineObjects;

namespace ConcreteEngine.Editor.UI;

internal sealed class LightingPanel : EditorPanel
{
    public LightingPanel(StateContext context) : base(PanelId.Lighting, context)
    {
        ShadowFields.ShadowSizeCombo.Layout = FieldLayout.None;
    }

    public override void Enter()
    {
        LightFields.Direction.Refresh();
        LightFields.Diffuse.Refresh();
        LightFields.Intensity.Refresh();
        LightFields.Specular.Refresh();
        LightFields.Ambient.Refresh();
        LightFields.AmbientGround.Refresh();
        LightFields.Exposure.Refresh();

        ShadowFields.ShadowProjectionFields.Refresh();
        ShadowFields.ShadowVisualFields.Refresh();
        ShadowFields.ShadowSizeCombo.Refresh();
    }

    public override void Draw(FrameContext ctx)
    {
        ImGui.SeparatorText("Illumination"u8);

        if (!ImGui.BeginTabBar("##tabs"u8)) return;

        if (ImGui.BeginTabItem("Light"u8))
        {
            DrawLight();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Shadow"u8))
        {
            DrawShadow();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }


    private static void DrawLight()
    {
        ImGui.SeparatorText("Directional Light"u8);
        LightFields.Direction.Draw();
        LightFields.Diffuse.Draw();
        LightFields.Intensity.Draw();
        LightFields.Specular.Draw();

        ImGui.Spacing();
        ImGui.SeparatorText("Ambient Light"u8);
        LightFields.Ambient.Draw();
        LightFields.AmbientGround.Draw();
        LightFields.Exposure.Draw();
    }


    private static void DrawShadow()
    {
        ImGui.SeparatorText("Shadow Map Size"u8);

        ShadowFields.ShadowSizeCombo.Draw();

        ImGui.SeparatorText("Shadow Projection"u8);
        ShadowFields.ShadowProjectionFields.Draw();

        ImGui.Spacing();
        ImGui.Separator();

        ImGui.SeparatorText("Shadow Visuals"u8);
        ShadowFields.ShadowVisualFields.Draw();
    }
}

file static class LightFields
{
    public static readonly FloatField<Float3Value> Direction = new("Direction", FieldWidgetKind.Drag,
        static () => Visuals.GetDirectionalLight().Direction,
        static value => Visuals.SetDirectionalLight(Visuals.GetDirectionalLight() with { Direction = (Vector3)value }))
    {
        Format = "%.2f",
        Delay = FieldGetDelay.High,
        Speed = 0.01f,
        Min = -1f,
        Max = 1f
    };

    public static readonly ColorField Diffuse = new("Diffuse", false,
        static () => (Color4)Visuals.GetDirectionalLight().Diffuse,
        static value => Visuals.SetDirectionalLight(Visuals.GetDirectionalLight() with { Diffuse = (Vector3)value }))
    {
        Delay = FieldGetDelay.High
    };

    public static readonly FloatField<Float1Value> Intensity = new("Intensity", FieldWidgetKind.Drag,
        static () => Visuals.GetDirectionalLight().Intensity,
        static value => Visuals.SetDirectionalLight(Visuals.GetDirectionalLight() with { Intensity = (float)value }))
    {
        Format = "%.3f",
        Delay = FieldGetDelay.High,
        Speed = 0.01f,
        Min = 0f,
        Max = 10f,
        Layout = FieldLayout.Inline
    };

    public static readonly FloatField<Float1Value> Specular = new("Specular", FieldWidgetKind.Drag,
        static () => Visuals.GetDirectionalLight().Specular,
        static value => Visuals.SetDirectionalLight(Visuals.GetDirectionalLight() with { Specular = (float)value }))
    {
        Format = "%.3f",
        Delay = FieldGetDelay.High,
        Speed = 0.01f,
        Min = 0f,
        Max = 10f,
        Layout = FieldLayout.Inline
    };

    public static readonly ColorField Ambient = new("Ambient", false,
        static () => (Color4)Visuals.GetAmbient().Ambient,
        static value => Visuals.SetAmbient(Visuals.GetAmbient() with { Ambient = (Vector3)value }))
    {
        Delay = FieldGetDelay.High
    };

    public static readonly ColorField AmbientGround = new("Ambient Ground", false,
        static () => (Color4)Visuals.GetAmbient().AmbientGround,
        static value => Visuals.SetAmbient(Visuals.GetAmbient() with { AmbientGround = (Vector3)value }))
    {
        Delay = FieldGetDelay.High
    };

    public static readonly FloatField<Float1Value> Exposure = new("Exposure", FieldWidgetKind.Drag,
        static () => Visuals.GetAmbient().Exposure,
        static value => Visuals.SetAmbient(Visuals.GetAmbient() with { Exposure = (float)value }))
    {
        Format = "%.3f",
        Delay = FieldGetDelay.High,
        Speed = 0.01f,
        Min = 0f,
        Max = 2f,
        Layout = FieldLayout.Inline
    };
}

file static class ShadowFields
{
    public static readonly ComboField ShadowSizeCombo = new ComboField("Shadow Size",
        [1024, 2048, 4096, 8192], ["1024px", "2048px", "4096px", "8192px"],
        static () => Visuals.GetShadow().ShadowMapSize,
        static value => Visuals.SetShadowSize((int)value)
    ).WithProperties(FieldGetDelay.VeryHigh, FieldLayout.None).WithPlaceholder("No Shadow");

    public static readonly FloatGroupField<Float4Value> ShadowProjectionFields = new FloatGroupField<Float4Value>(
            "Shadow Projection",
            static () =>
            {
                ref readonly var it = ref Visuals.GetShadow();
                return new Float4Value(it.Distance, it.ZPad, it.ConstBias, it.SlopeBias);
            },
            static value => Visuals.SetShadow(Visuals.GetShadow() with
            {
                Distance = value.X, ZPad = value.Y, ConstBias = value.Z, SlopeBias = value.W
            })
        ).WithProperties(FieldGetDelay.VeryHigh)
        .WithSlider("Distance", 10f, 500f)
        .WithSlider("Z-Padding", 0f, 100f)
        .WithDrag("Const Bias", 0.001f, 0.0001f, 0.01f, "%.4f")
        .WithDrag("Slope Bias", 0.001f, 0.001f, 0.01f, "%.4f");

    public static readonly FloatGroupField<Float2Value> ShadowVisualFields = new FloatGroupField<Float2Value>(
            "Shadow Visual",
            static () =>
            {
                ref readonly var it = ref Visuals.GetShadow();
                return new Float2Value(it.Strength, it.PcfRadius);
            },
            static value => Visuals.SetShadow(Visuals.GetShadow() with { Strength = value.X, PcfRadius = value.Y })
        ).WithProperties(FieldGetDelay.VeryHigh)
        .WithSlider("Strength", 0f, 1f).WithSlider("PcfRadius", 0.5f, 4f);

}