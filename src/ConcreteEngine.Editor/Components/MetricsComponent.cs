using System.Numerics;
using ConcreteEngine.Editor.Metrics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components;

public static class MetricsComponent
{
    public static void DrawMetricLeft()
    {
        if (MetricsApi.Store.Assets is not null)
            AssetStoreMetricsGui.DrawAssetStoreMetrics();

        ImGui.Dummy(new Vector2(0, 6));

        if (MetricsApi.Store.Gfx is not null)
            GfxStoreMetricsGui.DrawGfxStoreMetrics();
    }

    public static void DrawMetricRight() => SystemMetricsGui.Draw();

}