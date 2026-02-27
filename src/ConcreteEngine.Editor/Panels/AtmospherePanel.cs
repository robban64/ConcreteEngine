using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Bridge.EngineObjects;

namespace ConcreteEngine.Editor.Panels;

internal sealed class AtmospherePanel( StateContext context) : EditorPanel(PanelId.Atmosphere, context)
{
    public override void Enter()
    {
        _fogColorField.Refresh();
        _fogHeightFields.Refresh();
        _fogOpticsFields.Refresh();
    }

    public override void Draw(FrameContext ctx)
    {
        ImGui.SeparatorText("Atmosphere"u8);

        if (!ImGui.BeginTabBar("##tabs"u8)) return;

        if (ImGui.BeginTabItem("Fog"u8))
        {
            DrawFog();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }


    private void DrawFog()
    {
        var width = ImGui.GetContentRegionAvail().X;

        ImGui.SeparatorText("Fog Properties"u8);
        _fogColorField.Draw(width);
        _fogHeightFields.Draw();

        ImGui.SeparatorText("Fog Optics"u8);
        _fogOpticsFields.Draw();
    }

    private readonly ColorField _fogColorField = new ColorField("FogColor", false,
            static () => (Color4)Visuals.GetFog().Color,
            static value => Visuals.SetFog(Visuals.GetFog() with { Color = (Vector3)value }))
        .WithDelay(PropertyGetDelay.VeryHigh);

    
    private readonly FloatGroupField<Float4Value> _fogHeightFields = new FloatGroupField<Float4Value>("Fog Height",
            static () =>
            {
                ref readonly var f = ref Visuals.GetFog();
                return new Float4Value(f.Density, f.BaseHeight, f.HeightFalloff, f.HeightInfluence);
            },
            static value => Visuals.SetFog(Visuals.GetFog() with
            {
                Density = value.X, BaseHeight = value.Y, HeightFalloff = value.Z, HeightInfluence = value.W
            })
        ).WithDelay(PropertyGetDelay.VeryHigh)
        .WithSlider("Density", 100, 1500, "%.5f").WithSlider("BaseHeight", -1000f, 1000f, "%.3f")
        .WithSlider("Falloff", 0.001f, 10000.0f, "%.3f").WithDrag("Influence", 0.001f, 0f, 1f, "%.3f");

    private readonly FloatGroupField<Float3Value> _fogOpticsFields = new FloatGroupField<Float3Value>("Fog Optics",
            static () =>
            {
                ref readonly var f = ref Visuals.GetFog();
                return new Float3Value(f.Scattering, f.Strength, f.MaxDistance);
            },
            static value => Visuals.SetFog(Visuals.GetFog() with
            {
                Scattering = value.X, Strength = value.Y, MaxDistance = value.Z
            })
        ).WithDelay(PropertyGetDelay.VeryHigh)
        .WithDrag("Scattering", 0.001f, 0f, 1f, "%.5f").WithDrag("Strength", 0.001f, 0f, 1f, "%.3f")
        .WithDrag("Distance", 1, 1f, 10000f, "%.0f");

}

file static class FogFields
{

    public static readonly FloatField<Float1Value> Density = new("Density", FieldWidgetKind.Slider,
        static () => Visuals.GetFog().Density,
        static value => Visuals.SetFog(Visuals.GetFog() with { Density = (float)value }))
    {
        Format = "%.5f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 100,
        Max = 1500,
        Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> BaseHeight = new("Base Height", FieldWidgetKind.Slider,
        static () => Visuals.GetFog().BaseHeight,
        static value => Visuals.SetFog(Visuals.GetFog() with { BaseHeight = (float)value }))
    {
        Format = "%.3f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = -1000f,
        Max = 1000f,
        Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> Falloff = new("Falloff", FieldWidgetKind.Slider,
        static () => Visuals.GetFog().HeightFalloff,
        static value => Visuals.SetFog(Visuals.GetFog() with { HeightFalloff = (float)value }))
    {
        Format = "%.3f",
        Delay = PropertyGetDelay.VeryHigh,
        Min = 0.001f,
        Max = 10000.0f,
        Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> Influence = new("Influence", FieldWidgetKind.Drag,
        static () => Visuals.GetFog().HeightInfluence,
        static value => Visuals.SetFog(Visuals.GetFog() with { HeightInfluence = (float)value }))
    {
        Format = "%.3f",
        Delay = PropertyGetDelay.VeryHigh,
        Speed = 0.001f,
        Min = 0f,
        Max = 1f,
        Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> Scattering = new("Scattering", FieldWidgetKind.Drag,
        static () => Visuals.GetFog().Scattering,
        static value => Visuals.SetFog(Visuals.GetFog() with { Scattering = (float)value }))
    {
        Format = "%.3f",
        Delay = PropertyGetDelay.VeryHigh,
        Speed = 0.001f,
        Min = 0f,
        Max = 1f,
        Layout = FieldLabelLayout.Inline
    };


    public static readonly FloatField<Float1Value> Strength = new("Strength", FieldWidgetKind.Drag,
        static () => Visuals.GetFog().Strength,
        static value => Visuals.SetFog(Visuals.GetFog() with { Strength = (float)value }))
    {
        Format = "%.3f",
        Delay = PropertyGetDelay.VeryHigh,
        Speed = 0.001f,
        Min = 0f,
        Max = 1f,
        Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> MaxDistance = new("Max Distance", FieldWidgetKind.Slider,
        static () => Visuals.GetFog().MaxDistance,
        static value => Visuals.SetFog(Visuals.GetFog() with { MaxDistance = (float)value }))
    {
        Format = "%.0f", Delay = PropertyGetDelay.VeryHigh, Min = 1f, Max = 10000f
    };
}