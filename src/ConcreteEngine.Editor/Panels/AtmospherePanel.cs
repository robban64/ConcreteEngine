using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Bridge.EngineObjects;

namespace ConcreteEngine.Editor.Panels;

internal sealed class AtmospherePanel : EditorPanel
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

        FogFields.FogColor.Draw(width);

        FogFields.Density.Draw();
        FogFields.BaseHeight.Draw();
        FogFields.Falloff.Draw();
        FogFields.Influence.Draw();

        ImGui.SeparatorText("Fog Optics"u8);
        FogFields.Scattering.Draw();
        FogFields.Strength.Draw();
        FogFields.MaxDistance.Draw(width);
    }

    private readonly FloatGroupField<Float4Value> _fogHeight = new("Fog Height", FieldWidgetKind.Slider,
        static () =>
        {
            ref readonly var f = ref Visuals.GetFog();
            return new Float4Value(f.Density, f.BaseHeight, f.HeightFalloff, f.HeightInfluence);
        },
        static value => Visuals.SetFog(Visuals.GetFog() with
        {
            Density = value.X, BaseHeight = value.Y, HeightFalloff = value.Z, HeightInfluence = value.W
        })
    ) { Delay = PropertyGetDelay.VeryHigh };

    private readonly FloatGroupField<Float4Value> _fogOptics = new("Fog Optics", FieldWidgetKind.Drag,
        static () =>
        {
            ref readonly var f = ref Visuals.GetFog();
            return new Float4Value(f.Scattering, f.Strength, f.MaxDistance);
        },
        static value => Visuals.SetFog(Visuals.GetFog() with
        {
            Scattering = value.X, Strength = value.Y, MaxDistance = value.Z
        })
    ) { Delay = PropertyGetDelay.VeryHigh };
    
    public AtmospherePanel(StateContext context) : base(PanelId.Atmosphere, context)
    {
        _fogHeight.AddField(new FloatGroupEntry("Density", 100,1500, "%.5f"));
        _fogHeight.AddField(new FloatGroupEntry("BaseHeight", -1000f,1000f, "%.3f"));
        _fogHeight.AddField(new FloatGroupEntry("Falloff", 0.001f,10000.0f, "%.3f"));
        _fogHeight.AddField(new FloatGroupEntry("Influence",0.001f, 0f,1f, "%.3f"));

        _fogOptics.AddField(new FloatGroupEntry("Scattering", 0.001f, 0f,1f, "%.5f"));
        _fogOptics.AddField(new FloatGroupEntry("Strength", 0.001f, 0f,1f, "%.3f"));
        _fogOptics.AddField(new FloatGroupEntry("MaxDistance", 1,1f,10000f, "%.0f"));

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