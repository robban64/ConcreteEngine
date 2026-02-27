using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Bridge.EngineObjects;

namespace ConcreteEngine.Editor.Panels;

internal sealed class AtmospherePanel(StateContext context) : EditorPanel(PanelId.Atmosphere, context)
{
    public override void Enter()
    {
        FogFields.Strength.Refresh();
        FogFields.BaseHeight.Refresh();
        FogFields.Density.Refresh();
        FogFields.Falloff.Refresh();
        FogFields.FogColor.Refresh();
        FogFields.Influence.Refresh();
        FogFields.MaxDistance.Refresh();
        FogFields.Scattering.Refresh();
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


    private static void DrawFog()
    {
        var width = ImGui.GetContentRegionAvail().X;

        ImGui.SeparatorText("Fog Properties"u8);

        //FogFields.FogColor.DrawField(true, width);
        FogFields.FogColor.Draw(width);

        FogFields.Density.Draw();

        ImGui.SeparatorText("Fog Height"u8);
        FogFields.BaseHeight.Draw();
        FogFields.Falloff.Draw();
        FogFields.Influence.Draw();

        ImGui.SeparatorText("Fog Optics"u8);
        FogFields.Scattering.Draw();
        FogFields.Strength.Draw();
        FogFields.MaxDistance.Draw( width);
        //FogFields.MaxDistance.DrawField(true, width);
    }
}

file static class FogFields
{
    public static readonly ColorField FogColor = new("FogColor", false,
        static () => (Color4)Visuals.GetFog().Color,
        static value => Visuals.SetFog(Visuals.GetFog() with { Color = (Vector3)value }))
    {
        Delay = PropertyGetDelay.VeryHigh
    };

    public static readonly FloatField<Float1Value> Density = new("Density", FieldWidgetKind.Slider,
        static () => Visuals.GetFog().Density,
        static value => Visuals.SetFog(Visuals.GetFog() with { Density = (float)value }))
    {
        Format = "%.5f", Delay = PropertyGetDelay.VeryHigh, Min = 100, Max = 1500, Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> BaseHeight = new("Base Height", FieldWidgetKind.Slider,
        static () => Visuals.GetFog().BaseHeight,
        static value => Visuals.SetFog(Visuals.GetFog() with { BaseHeight = (float)value }))
    {
        Format = "%.3f", Delay = PropertyGetDelay.VeryHigh, Min = -1000f, Max = 1000f, Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> Falloff = new("Falloff", FieldWidgetKind.Slider,
        static () => Visuals.GetFog().HeightFalloff,
        static value => Visuals.SetFog(Visuals.GetFog() with { HeightFalloff = (float)value }))
    {
        Format = "%.3f", Delay = PropertyGetDelay.VeryHigh, Min = 0.001f, Max = 10000.0f, Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> Influence = new("Influence", FieldWidgetKind.Drag,
        static () => Visuals.GetFog().HeightInfluence,
        static value => Visuals.SetFog(Visuals.GetFog() with { HeightInfluence = (float)value }))
    {
        Format = "%.3f", Delay = PropertyGetDelay.VeryHigh, Speed = 0.001f, Min = 0f, Max = 1f, Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> Scattering = new("Scattering", FieldWidgetKind.Drag,
        static () => Visuals.GetFog().Scattering,
        static value => Visuals.SetFog(Visuals.GetFog() with { Scattering = (float)value }))
    {
        Format = "%.3f", Delay = PropertyGetDelay.VeryHigh,Speed = 0.001f, Min = 0f, Max = 1f, Layout = FieldLabelLayout.Inline
    };
    
    
    public static readonly FloatField<Float1Value> Strength = new("Strength",FieldWidgetKind.Drag,
        static () => Visuals.GetFog().Strength,
        static value => Visuals.SetFog(Visuals.GetFog() with { Strength = (float)value }))
    {
        Format = "%.3f", Delay = PropertyGetDelay.VeryHigh,Speed = 0.001f, Min = 0f, Max = 1f, Layout = FieldLabelLayout.Inline
    };

    public static readonly FloatField<Float1Value> MaxDistance = new("Max Distance",FieldWidgetKind.Slider,
        static () => Visuals.GetFog().MaxDistance,
        static value => Visuals.SetFog(Visuals.GetFog() with { MaxDistance = (float)value }))
    {
        Format = "%.0f", Delay = PropertyGetDelay.VeryHigh, Min = 1f,  Max = 10000f
    };

}