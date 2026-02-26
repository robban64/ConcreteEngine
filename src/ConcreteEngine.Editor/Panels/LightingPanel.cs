using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Bridge.EngineObjects;

namespace ConcreteEngine.Editor.Panels;

internal sealed class LightingPanel(StateContext context) : EditorPanel(PanelId.Lighting, context)
{
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
        LightFields.Direction.DrawField(true, width);
        LightFields.Diffuse.DrawField(true, width);
        LightFields.Intensity.DrawField(false);
        LightFields.Specular.DrawField(false);

        ImGui.Spacing();
        ImGui.SeparatorText("Ambient Light"u8);
        LightFields.Ambient.DrawField(true, width);
        LightFields.AmbientGround.DrawField(true, width);
        LightFields.Exposure.DrawField(false);
    }


    private static void DrawShadow()
    {
        var width = ImGui.GetContentRegionAvail().X;

        ImGui.SeparatorText("Shadow Map Size"u8);

        ShadowFields.ShadowSizeCombo.DrawField(true, width);

        ImGui.SeparatorText("Shadow Properties"u8);

        ShadowFields.Distance.DrawField(false);
        ShadowFields.Strength.DrawField(false);

        ImGui.Spacing();
        ImGui.Separator();
        ShadowFields.ConstBias.DrawField(false);
        ShadowFields.PcfRadius.DrawField(false);
        ShadowFields.SlopeBias.DrawField(false);
        ShadowFields.ZPad.DrawField(false);
    }
}

file static class LightFields
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

file static class ShadowFields
{
    public static readonly ComboField ShadowSizeCombo = new ComboField("Shadow Size",
        [1024, 2048, 4096, 8192], ["1024px", "2048px", "4096px", "8192px"],
        static () => Visuals.GetShadow().ShadowMapSize,
        static value => Visuals.SetShadowSize(value)
    ).WithPlaceholder("No Shadow");

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