using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Inspectors;

internal sealed class LightingPanel
{
    private readonly IlluminationSettingsInspector _illuminationSettingsInspector = new();
    private readonly ShadowSettingsInspector _shadowSettingsInspector = new();
    private readonly EnvironmentSettingsInspector _environmentSettingsInspector = new();

    public void Draw()
    {
        ImGui.SeparatorText("Illumination"u8);

        if (!ImGui.BeginTabBar("##tabs"u8)) return;

        if (ImGui.BeginTabItem("Light"u8))
        {
            _illuminationSettingsInspector.DrawDirectionalLight();
            _illuminationSettingsInspector.DrawAmbient();

            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Shadow"u8))
        {
            _shadowSettingsInspector.Draw();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Fog"u8))
        {
            _environmentSettingsInspector.Draw();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }
/*

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
    */
}