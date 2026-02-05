using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Metrics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Metrics;

internal sealed class MetricsLeftPanel(PanelContext context) : EditorPanel(PanelId.MetricsLeft, context)
{
    private const ImGuiChildFlags Flags = ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.AlwaysUseWindowPadding;

    public override void Enter() => MetricsApi.EnterMetricMode();
    public override void Leave() => MetricsApi.LeaveMetricMode();
    public override void UpdateDiagnostic() => MetricsApi.Tick();

    public override void Draw(in FrameContext ctx)
    {
        ImGui.BeginChild("##metrics-asset"u8, Flags);
        if (MetricsApi.Store.Assets is not null)
            DrawAssetStoreMetrics.Draw(in ctx);

        ImGui.EndChild();

        ImGui.Dummy(new Vector2(0, 6));

        ImGui.BeginChild("##metrics-gfx"u8, Flags);
        if (MetricsApi.Store.Gfx is not null)
            DrawGfxStoreMetrics.Draw(in ctx);

        ImGui.EndChild();
    }
}

internal sealed class MetricsRightPanel(PanelContext context) : EditorPanel(PanelId.MetricsRight, context)
{
    private const ImGuiChildFlags Flags = ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.AlwaysUseWindowPadding;

    private GcActivity _gcActivity;
    private float _gcCooldown;

    public override void Draw(in FrameContext ctx)
    {
        ImGui.BeginChild("##metrics-right"u8, Flags);

        scoped ref readonly var performance = ref MetricsApi.Provider<PerformanceMetric>.Data;
        TickGcActivity(ctx.DeltaTime, performance.GcActivity);

        DrawSystemMetrics.DrawFrameMeta(in ctx);
        DrawSystemMetrics.DrawMetrics(in ctx);
        DrawSystemMetrics.DrawSession(in ctx, performance.AllocMbPerSec);
        DrawSystemMetrics.DrawFooter();

        ImGui.EndChild();
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