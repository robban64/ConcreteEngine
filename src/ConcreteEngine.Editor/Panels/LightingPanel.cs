using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Bridge.EngineObjects;

namespace ConcreteEngine.Editor.Panels;

internal sealed class LightingPanel : EditorPanel
{
    public LightingPanel(StateContext context) : base(PanelId.Lighting, context)
    {
        ShadowFields.ShadowSizeCombo.Layout = FieldLabelLayout.None;
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

        ShadowFields.Distance.Refresh();
        ShadowFields.Strength.Refresh();
        ShadowFields.ConstBias.Refresh();
        ShadowFields.PcfRadius.Refresh();
        ShadowFields.SlopeBias.Refresh();
        ShadowFields.ZPad.Refresh();
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
        var width = ImGui.GetContentRegionAvail().X;

        ImGui.SeparatorText("Directional Light"u8);
        LightFields.Direction.Draw(width);
        LightFields.Diffuse.Draw(width);
        LightFields.Intensity.Draw();
        LightFields.Specular.Draw();

        ImGui.Spacing();
        ImGui.SeparatorText("Ambient Light"u8);
        LightFields.Ambient.Draw(width);
        LightFields.AmbientGround.Draw(width);
        LightFields.Exposure.Draw();
    }


    private static void DrawShadow()
    {
        var width = ImGui.GetContentRegionAvail().X;

        ImGui.SeparatorText("Shadow Map Size"u8);

        //ShadowFields.ShadowSizeCombo.DrawField(true, width);
        ShadowFields.ShadowSizeCombo.Draw(width);

        ImGui.SeparatorText("Shadow Properties"u8);

        ShadowFields.Distance.Draw();
        ShadowFields.Strength.Draw();

        ImGui.Spacing();
        ImGui.Separator();
        ShadowFields.ConstBias.Draw();
        ShadowFields.PcfRadius.Draw();
        ShadowFields.SlopeBias.Draw();
        ShadowFields.ZPad.Draw();
    }
}

file static class LightFields
{
    public static readonly FloatField<Float3Value> Direction = new("Direction", FieldWidgetKind.Drag,
        static () => Visuals.GetDirectionalLight().Direction,
        static value => Visuals.SetDirectionalLight(Visuals.GetDirectionalLight() with { Direction = (Vector3)value }))
    {
        Format = "%.2f",
        Delay = PropertyGetDelay.High,
        Speed = 0.01f,
        Min = -1f,
        Max = 1f
    };

    public static readonly ColorField Diffuse = new("Diffuse", false,
        static () => (Color4)Visuals.GetDirectionalLight().Diffuse,
        static value => Visuals.SetDirectionalLight(Visuals.GetDirectionalLight() with { Diffuse = (Vector3)value }))
    {
        Delay = PropertyGetDelay.High
    };

    public static readonly FloatField<Float1Value> Intensity = new("Intensity", FieldWidgetKind.Drag,
        static () => Visuals.GetDirectionalLight().Intensity,
        static value => Visuals.SetDirectionalLight(Visuals.GetDirectionalLight() with { Intensity = (float)value }))
    {
        Format = "%.3f",
        Delay = PropertyGetDelay.High,
        Speed = 0.01f,
        Min = 0f,
        Max = 10f,
        Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> Specular = new("Specular", FieldWidgetKind.Drag,
        static () => Visuals.GetDirectionalLight().Specular,
        static value => Visuals.SetDirectionalLight(Visuals.GetDirectionalLight() with { Specular = (float)value }))
    {
        Format = "%.3f",
        Delay = PropertyGetDelay.High,
        Speed = 0.01f,
        Min = 0f,
        Max = 10f,
        Layout = FieldLabelLayout.Inline
    };

    public static readonly ColorField Ambient = new("Ambient", false,
        static () => (Color4)Visuals.GetAmbient().Ambient,
        static value => Visuals.SetAmbient(Visuals.GetAmbient() with { Ambient = (Vector3)value }))
    {
        Delay = PropertyGetDelay.High
    };

    public static readonly ColorField AmbientGround = new("Ambient Ground", false,
        static () => (Color4)Visuals.GetAmbient().AmbientGround,
        static value => Visuals.SetAmbient(Visuals.GetAmbient() with { AmbientGround = (Vector3)value }))
    {
        Delay = PropertyGetDelay.High
    };

    public static readonly FloatField<Float1Value> Exposure = new("Exposure", FieldWidgetKind.Drag,
        static () => Visuals.GetAmbient().Exposure,
        static value => Visuals.SetAmbient(Visuals.GetAmbient() with { Exposure = (float)value }))
    {
        Format = "%.3f",
        Delay = PropertyGetDelay.High,
        Speed = 0.01f,
        Min = 0f,
        Max = 2f,
        Layout = FieldLabelLayout.Inline
    };
}

file static class ShadowFields
{
    public static readonly ComboField ShadowSizeCombo = new ComboField("Shadow Size",
        [1024, 2048, 4096, 8192], ["1024px", "2048px", "4096px", "8192px"],
        static () => Visuals.GetShadow().ShadowMapSize,
        static value => Visuals.SetShadowSize((int)value)
    ).WithPlaceholder("No Shadow");

    public static readonly FloatField<Float1Value> Distance = new("Distance", FieldWidgetKind.Slider,
        static () => Visuals.GetShadow().Distance,
        static value => Visuals.SetShadow(Visuals.GetShadow() with { Distance = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh, Min = 10f, Max = 200f,Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> ZPad = new("ZPad", FieldWidgetKind.Slider,
        static () => Visuals.GetShadow().ZPad,
        static value => Visuals.SetShadow(Visuals.GetShadow() with { ZPad = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh, Min = 1f, Max = 200f,Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> ConstBias = new("ConstBias", FieldWidgetKind.Slider,
        static () => Visuals.GetShadow().ConstBias,
        static value => Visuals.SetShadow(Visuals.GetShadow() with { ConstBias = (float)value }))
    {
        Format = "%.5f", Delay = PropertyGetDelay.VeryHigh, Min = 0.0001f, Max = 0.001f,Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> SlopeBias = new("SlopeBias", FieldWidgetKind.Slider,
        static () => Visuals.GetShadow().SlopeBias,
        static value => Visuals.SetShadow(Visuals.GetShadow() with { SlopeBias = (float)value }))
    {
        Format = "%.4f", Delay = PropertyGetDelay.VeryHigh, Min = 0.001f, Max = 0.01f,Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> Strength = new("Strength", FieldWidgetKind.Slider,
        static () => Visuals.GetShadow().Strength,
        static value => Visuals.SetShadow(Visuals.GetShadow() with { Strength = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh, Min = 0f, Max = 1f,Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> PcfRadius = new("PcfRadius", FieldWidgetKind.Slider,
        static () => Visuals.GetShadow().PcfRadius,
        static value => Visuals.SetShadow(Visuals.GetShadow() with { PcfRadius = (float)value }))
    {
        Format = "%.2f", Delay = PropertyGetDelay.VeryHigh, Min = 0.5f, Max = 4f,Layout = FieldLabelLayout.Inline
    };
}