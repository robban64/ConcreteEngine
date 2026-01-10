using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Metrics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components;

internal sealed class MetricsComponent : EditorComponent<EmptyState>
{
    public override void DrawLeft(EmptyState state)
    {
        if (MetricsApi.Store.Assets is not null)
            AssetStoreMetricsGui.DrawAssetStoreMetrics();

        ImGui.Dummy(new Vector2(0, 6));

        if (MetricsApi.Store.Gfx is not null)
            GfxStoreMetricsGui.DrawGfxStoreMetrics();

    }

    public override void DrawRight(EmptyState state)
    {
        SystemMetricsGui.Draw();
    }

}