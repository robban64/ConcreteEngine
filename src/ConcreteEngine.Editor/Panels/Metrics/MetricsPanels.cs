using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Metrics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Metrics;

internal sealed class MetricsLeftPanel(StateContext context) : EditorPanel(PanelId.MetricsLeft, context)
{
    public override void Enter()
    {
        MetricSystem.Instance.FastMode = true;
        MetricSystem.Instance.Stores?.Refresh();
    }

    public override void Leave()
    {
        MetricSystem.Instance.FastMode = false;
    }

    public override void Draw(FrameContext ctx)
    {
        if (MetricSystem.Instance.Stores is not { } stores) return;
        ImGui.BeginChild("metrics-asset"u8, ImGuiChildFlags.AutoResizeY);
        DrawAssetStoreMetrics.Draw(ctx, stores.Assets);

        ImGui.EndChild();

        ImGui.Dummy(new Vector2(0, 6));

        ImGui.BeginChild("metrics-gfx"u8, ImGuiChildFlags.AutoResizeY);
        DrawGfxStoreMetrics.Draw(ctx, stores.Gfx, stores.GfxMetaDescriptions);

        ImGui.EndChild();
    }
}

internal sealed class MetricsRightPanel(StateContext context) : EditorPanel(PanelId.MetricsRight, context)
{
    private GcActivity _gcActivity;
    private float _gcCooldown;

    public override void Draw(FrameContext ctx)
    {
        ImGui.PushID("metrics-right"u8);

       // TickGcActivity(EditorTime.DeltaTime, MetricSystem.Instance.RuntimeMetric.GcActivity);

        DrawSystemMetrics.DrawFrameMeta(ctx);
        DrawSystemMetrics.DrawPerformanceMetrics(ctx);
        /*
        ImGui.Dummy(new Vector2(0, 4));
        DrawSystemMetrics.DrawSession(ctx, MetricSystem.Instance.RuntimeMetric.AllocMbPerSec);
        ImGui.Dummy(new Vector2(0, 4));
        DrawSystemMetrics.DrawFooter();
        */

        ImGui.PopID();
    }

    private void TickGcActivity(float delta, GcActivity activity)
    {
        if (_gcActivity == GcActivity.None && activity == GcActivity.None) return;

        if (_gcActivity != activity)
        {
            _gcActivity = activity;
            _gcCooldown = 4;
        }

        _gcCooldown -= delta;
        if (_gcCooldown <= 0)
        {
            _gcActivity = GcActivity.None;
            _gcCooldown = 0;
        }
    }
}