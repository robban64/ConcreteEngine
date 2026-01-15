using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor.Components.Draw;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Metrics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components;

internal sealed class MetricsComponent : EditorComponent<EmptyState>
{
    private const ImGuiChildFlags Flags = ImGuiChildFlags.AutoResizeY;

    private GcActivity _gcActivity;
    private float _gcCooldown;


    public override void DrawLeft(EmptyState state, in FrameContext ctx)
    {
        var writer = ctx.Writer;
        if (ImGui.BeginChild("##metrics-asset"u8, Flags))
        {
            if (MetricsApi.Store.Assets is not null)
                DrawAssetStoreMetrics.Draw(ref writer);
        }

        ImGui.EndChild();

        ImGui.Dummy(new Vector2(0, 6));

        if (ImGui.BeginChild("##metrics-gfx"u8, Flags))
        {
            if (MetricsApi.Store.Gfx is not null)
                DrawGfxStoreMetrics.Draw(ref writer);
        }

        ImGui.EndChild();
    }

    public override void DrawRight(EmptyState state, in FrameContext ctx)
    {
        if (!ImGui.BeginChild("##metrics-right"u8, Flags))
            return;

        var sw = ctx.Writer;
        ref readonly var performance = ref MetricsApi.Provider<PerformanceMetric>.Data;
        TickGcActivity(ctx.DeltaTime, performance.GcActivity);

        DrawSystemMetrics.DrawFrameMeta(ref sw);
        DrawSystemMetrics.DrawMetrics(ref sw);
        DrawSystemMetrics.DrawSession(ref sw, performance.AllocMbPerSec);

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