using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Inspectors;

internal sealed class LightingPanel
{
    private readonly DirectionalLightInspector _directionalLightInspector = new();
    private readonly ShadowSettingsInspector _shadowSettingsInspector = new();
    private readonly EnvironmentSettingsInspector _environmentSettingsInspector = new();

}