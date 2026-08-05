using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Inspectors;

internal sealed class LightingPanel
{
    private readonly DirectionalLightInspector _directionalLightInspector = new();
    private readonly ShadowSettingsInspector _shadowSettingsInspector = new();
    private readonly EnvironmentSettingsInspector _environmentSettingsInspector = new();

    public void Draw()
    {
        ImGui.SeparatorText("Illumination"u8);

        if (!ImGui.BeginTabBar("##tabs"u8)) return;

        if (ImGui.BeginTabItem("Sun"u8))
        {
            _directionalLightInspector.Draw();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Shadow"u8))
        {
            _shadowSettingsInspector.Draw();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Environment"u8))
        {
            _environmentSettingsInspector.Draw();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }
}