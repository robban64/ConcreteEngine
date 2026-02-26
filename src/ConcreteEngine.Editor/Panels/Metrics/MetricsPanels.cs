using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Metrics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Metrics;

internal sealed class MetricsLeftPanel(StateContext context) : EditorPanel(PanelId.MetricsLeft, context)
{
    public override void Enter() => MetricsApi.EnterMetricMode();
    public override void Leave() => MetricsApi.LeaveMetricMode();
    public override void UpdateDiagnostic() => MetricsApi.Tick();

    public override void Draw(FrameContext ctx)
    {
        ImGui.BeginChild("metrics-asset"u8, ImGuiChildFlags.AutoResizeY);
        if (MetricsApi.Store.Assets is not null)
            DrawAssetStoreMetrics.Draw(ctx);

        ImGui.EndChild();

        ImGui.Dummy(new Vector2(0, 6));

        ImGui.BeginChild("metrics-gfx"u8, ImGuiChildFlags.AutoResizeY);
        if (MetricsApi.Store.Gfx is not null)
            DrawGfxStoreMetrics.Draw(ctx);

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

        scoped ref readonly var performance = ref MetricsApi.Provider<PerformanceMetric>.Data;
        TickGcActivity(EditorTime.DeltaTime, performance.GcActivity);

        DrawSystemMetrics.DrawFrameMeta(ctx);
        DrawSystemMetrics.DrawMetrics(ctx);
        ImGui.Dummy(new Vector2(0, 4));
        DrawSystemMetrics.DrawSession(ctx, performance.AllocMbPerSec);
        ImGui.Dummy(new Vector2(0, 4));
        DrawSystemMetrics.DrawFooter();

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