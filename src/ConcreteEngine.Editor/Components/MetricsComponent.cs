using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor.Components.Draw;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Metrics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components;

internal sealed class MetricsComponent : EditorComponent<EmptyState>
{
    private const int WindowPaddingX = 12;

    private GcActivity _gcActivity;
    private float _gcCooldown;

    public override void DrawLeft(EmptyState state,in FrameContext ctx)
    {
        if (!ImGui.BeginChild("##metrics-right"u8, ImGuiChildFlags.AlwaysUseWindowPadding))
            return;

        if (MetricsApi.Store.Assets is not null)
            DrawAssetStoreMetrics.Draw(ctx.Buffer);

        ImGui.Dummy(new Vector2(0, 6));

        if (MetricsApi.Store.Gfx is not null)
            DrawGfxStoreMetrics.Draw(ctx.Buffer);
        
        ImGui.EndChild();
    }

    public override void DrawRight(EmptyState state,in FrameContext ctx)
    {
        if (!ImGui.BeginChild("##metrics-right"u8, ImGuiChildFlags.AlwaysUseWindowPadding))
            return;

        TickGcActivity(ctx.DeltaTime, MetricsApi.Provider<PerformanceMetric>.Data.GcActivity);

        var allocRate = MetricsApi.Provider<PerformanceMetric>.Data.AllocMbPerSec;
        DrawSystemMetrics.DrawFrameMeta(ctx.Buffer);
        DrawSystemMetrics.DrawMetrics(ctx.Buffer);
        DrawSystemMetrics.DrawSession(ctx.Buffer, allocRate);

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