using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Inspectors;

internal sealed class LightingPanel
{
    private readonly LightingSettingsInspector _lightingSettingsInspector = new();
    private readonly ShadowSettingsInspector _shadowSettingsInspector = new();
    private readonly FogSettingsInspector _fogSettingsInspector = new();

}