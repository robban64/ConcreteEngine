using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Provider;
using ConcreteEngine.Editor.Core.Provider.Impl;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Inspection;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Inspectors;

internal sealed class LightingPanel : EditorPanel
{
    
    private IlluminationSettingsInspector _illuminationSettingsInspector;
    private ShadowSettingsInspector _shadowSettingsInspector;
    private EnvironmentSettingsInspector _environmentSettingsInspector;

    public LightingPanel(StateManager state) : base(InspectorId.Lighting, state)
    {
        _illuminationSettingsInspector = new IlluminationSettingsInspector();
        _shadowSettingsInspector = new ShadowSettingsInspector();
        _environmentSettingsInspector = new EnvironmentSettingsInspector();
        //_inspectFields.ShadowSizeCombo.LabelPlacement = LabelPlacement.None;
    }

    public override void OnDraw()
    {
        ImGui.SeparatorText("Illumination"u8);

        if (!ImGui.BeginTabBar("##tabs"u8)) return;

        if (ImGui.BeginTabItem("Light"u8))
        {
            IlluminationSettingsInspector.Instance.DrawDirectionalLight();
            IlluminationSettingsInspector.Instance.DrawAmbient();

            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Shadow"u8))
        {
            ShadowSettingsInspector.Instance.DrawProjection();
            ShadowSettingsInspector.Instance.DrawVisuals();

            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Fog"u8))
        {
            EnvironmentSettingsInspector.Instance.DrawFogHeight();
            EnvironmentSettingsInspector.Instance.DrawFogOptics();

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