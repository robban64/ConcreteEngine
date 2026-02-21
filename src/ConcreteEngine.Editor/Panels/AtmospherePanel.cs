using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Controller.EngineObjects;

namespace ConcreteEngine.Editor.Panels;

internal sealed class AtmospherePanel(PanelContext context) : EditorPanel(PanelId.Atmosphere, context)
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

    public override void Draw(in FrameContext ctx)
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

        FogFields.FogColor.DrawField(true, width);
        FogFields.Density.DrawField(false);

        ImGui.SeparatorText("Fog Height"u8);
        FogFields.BaseHeight.DrawField(false);
        FogFields.Falloff.DrawField(false);
        FogFields.Influence.DrawField(false);

        ImGui.SeparatorText("Fog Optics"u8);
        FogFields.Scattering.DrawField(false);
        FogFields.Strength.DrawField(false);
        FogFields.MaxDistance.DrawField(true, width);
    }
}

file static class FogFields
{
    public static readonly ColorInputField FogColor = new("FogColor", false,
        static () => (Color4)Visuals.GetFog().Color,
        static value => Visuals.SetFog(Visuals.GetFog() with { Color = value.ToVector3() }))
    {
        Delay = PropertyGetDelay.VeryHigh
    };

    public static readonly FloatSliderField<Float1Value> Density = new("Density", 100, 1500,
        static () => Visuals.GetFog().Density,
        static value => Visuals.SetFog(Visuals.GetFog() with { Density = (float)value }))
    {
        Format = "%.5f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> BaseHeight = new("Base Height", -1000f, 1000f,
        static () => Visuals.GetFog().BaseHeight,
        static value => Visuals.SetFog(Visuals.GetFog() with { BaseHeight = (float)value }))
    {
        Format = "%.3f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatSliderField<Float1Value> Falloff = new("Falloff", 0.001f, 10000.0f,
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

    public static readonly FloatSliderField<Float1Value> MaxDistance = new("Max Distance", 1f, 10000f,
        static () => Visuals.GetFog().MaxDistance,
        static value => Visuals.SetFog(Visuals.GetFog() with { MaxDistance = (float)value }))
    {
        Format = "%.0f", Delay = PropertyGetDelay.VeryHigh,
    };

    public static readonly FloatDragField<Float1Value> Strength = new("Strength", 0.001f, 0f, 1f,
        static () => Visuals.GetFog().Strength,
        static value => Visuals.SetFog(Visuals.GetFog() with { Strength = (float)value }))
    {
        Format = "%.3f", Delay = PropertyGetDelay.VeryHigh,
    };
}