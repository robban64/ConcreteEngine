using Core.DebugTools.Data;
using static Core.DebugTools.Utils.GuiUtils;

namespace Core.DebugTools.Gui.Metrics;

internal static class SceneMetricsGui
{
    public static void DrawSceneMetrics(DebugSceneMetricsText sceneMetrics)
    {
        DrawSectionHeader("Scene Metrics");
        TextIfNotNull(sceneMetrics.EntityCount);
        TextIfNotNull(sceneMetrics.ShadowMapSize);
    }
}