using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Bridge.EngineObjectStore;

namespace ConcreteEngine.Editor.UI;

internal sealed class AtmospherePanel(StateContext context) : EditorPanel(PanelId.Atmosphere, context)
{
    public override void OnEnter()
    {
        FogColorField.Refresh();
        FogHeightFields.Refresh();
        FogOpticsFields.Refresh();
    }

    public override void OnDraw(FrameContext ctx)
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
        ImGui.SeparatorText("Fog Properties"u8);
        FogColorField.Draw();
        FogHeightFields.Draw();

        ImGui.SeparatorText("Fog Optics"u8);
        FogOpticsFields.Draw();
    }

    private static readonly ColorField FogColorField = new ColorField("FogColor", false,
            static () => (Color4)Visuals.GetFog().Color,
            static value => Visuals.Mutate().Fog.Color = (Vector3)value)
        .WithProperties(FieldGetDelay.VeryHigh);


    private static readonly FloatGroupField<Float4Value> FogHeightFields = new FloatGroupField<Float4Value>(
            "Fog Height",
            static () =>
            {
                ref readonly var f = ref Visuals.GetFog();
                return new Float4Value(f.Density, f.BaseHeight, f.HeightFalloff, f.HeightInfluence);
            },
            static value =>
            {
                var mutate = Visuals.Mutate();
                mutate.Fog.Density = value.X;
                mutate.Fog.BaseHeight = value.Y;
                mutate.Fog.HeightFalloff = value.Z;
                mutate.Fog.HeightInfluence = value.W;
            })
        .WithProperties(FieldGetDelay.VeryHigh)
        .WithSlider("Density", 100, 1500, "%.5f").WithSlider("BaseHeight", -1000f, 1000f, "%.3f")
        .WithSlider("Falloff", 0.001f, 10000.0f, "%.3f").WithDrag("Influence", 0.001f, 0f, 1f, "%.3f");

    private static readonly FloatGroupField<Float3Value> FogOpticsFields = new FloatGroupField<Float3Value>(
            "Fog Optics",
            static () =>
            {
                ref readonly var f = ref Visuals.GetFog();
                return new Float3Value(f.Scattering, f.Strength, f.MaxDistance);
            },
            static value =>
            {
                var mutate = Visuals.Mutate();
                mutate.Fog.Scattering = value.X;
                mutate.Fog.Strength = value.Y;
                mutate.Fog.MaxDistance = value.Z;
            })
        .WithProperties(FieldGetDelay.VeryHigh)
        .WithDrag("Scattering", 0.001f, 0f, 1f, "%.5f").WithDrag("Strength", 0.001f, 0f, 1f, "%.3f")
        .WithDrag("Distance", 1, 1f, 10000f, "%.0f");
}