using ImGuiNET;
using static ConcreteEngine.Editor.Utils.GuiUtils;

namespace ConcreteEngine.Editor.Metrics;

internal static class SceneMetricsGui
{
    public static void DrawSceneMetrics(DebugSceneMetricsText sceneMetrics)
    {
        ImGui.SeparatorText("Scene Metrics");
        TextIfNotNull(sceneMetrics.EntityCount);
        TextIfNotNull(sceneMetrics.ShadowMapSize);
    }
}