using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;
using static ConcreteEngine.Editor.Utils.GuiUtils;

namespace ConcreteEngine.Editor.Metrics;

internal static class SystemMetricsGui
{
    private const int WindowPaddingX = 12;

    private static GcActivity _gcActivity;
    private static float _gcCooldown;

    public static void Draw(float delta)
    {
        const ImGuiChildFlags flags = ImGuiChildFlags.AlwaysUseWindowPadding;
        var size = new Vector2(GuiTheme.RightSidebarWidth - WindowPaddingX, 0);

        if (!ImGui.BeginChild("##system-metrics-gui", size, flags)) return;

        Span<byte> buffer = stackalloc byte[128];

        var allocRate = MetricsApi.Provider<PerformanceMetric>.Data.AllocMbPerSec;
        DrawFrameMeta(buffer);
        DrawMetrics(delta, buffer);
        DrawSession(buffer, allocRate);

        ImGui.EndChild();
    }

    private static void TickGcActivity(float delta, GcActivity activity)
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

    private static void DrawFrameMeta(Span<byte> buffer)
    {
        ref readonly var frameInfo = ref MetricsApi.Provider<FrameMeta>.Data;
        ref readonly var gpuMeta = ref MetricsApi.Provider<GpuFrameMetaBundle>.Data;
        
        var za = ZaUtf8SpanWriter.Create(buffer);

        // Frame Info
        ImGui.SeparatorText("Frame Info");
        MetricText(ref za, "Frame:", frameInfo.FrameId);
        MetricText(ref za, "FPS:", frameInfo.Fps, format: "F2");
        MetricText(ref za, "Alpha:", frameInfo.Alpha, format: "F2", suffix: "ms");


        // Render Frame 
        ImGui.SeparatorText("Render Info");
        MetricText(ref za, "Draws:", gpuMeta.Frame.Draws);
        MetricText(ref za, "Tris:", gpuMeta.Frame.Tris);

        ImGui.Dummy(new Vector2(0, 6));

    }

    private static void DrawMetrics(float delta, Span<byte> buffer)
    {
        ref readonly var metric = ref MetricsApi.Provider<PerformanceMetric>.Data;

        TickGcActivity(delta, metric.GcActivity);

        var za = ZaUtf8SpanWriter.Create(buffer);

        // Frame Metric
        ImGui.SeparatorText("Frame Metric");
        MetricText(ref za, "Avg:", metric.AvgMs, format: "F4", suffix: "ms");
        MetricText(ref za, "Max:", metric.MaxMs, format: "F4", suffix: "ms");
        MetricText(ref za, "Min:", metric.MinMs, format: "F4", suffix: "ms");
        MetricText(ref za, "Load:", metric.Load, format: "F4", suffix: "ms");

        ImGui.Dummy(new Vector2(0, 2));

        // Gc Metric
        ImGui.SeparatorText("GC Metric");
        MetricText(ref za, "Allocated:", metric.AllocatedMb, suffix: "MB", space: 70);
        MetricText(ref za, "AllocRate:", metric.AllocMbPerSec, format: "F4", suffix: "s", space: 70);

        var gc = metric.Gc;
        za.Clear();
        ImGui.TextUnformatted(za.Append("Generation: ").Append("(").Append(gc.Gen0).Append(", ").Append(gc.Gen1)
            .Append(", ").Append(gc.Gen2).Append(")").AsSpan());

        ImGui.TextUnformatted("GcActivity: ");
        ImGui.SameLine();
        switch (metric.GcActivity)
        {
            case GcActivity.None:
                ImGui.TextUnformatted("Idle");
                break;
            case GcActivity.Minor:
                ImGui.TextColored(Color4.Yellow, "Minor");
                break;
            case GcActivity.Major:
                ImGui.TextColored(Color4.Red, "Major");
                break;
        }

    }

    private static void DrawSession(Span<byte> buffer, float allocMbPerSec)
    {
        var za = ZaUtf8SpanWriter.Create(buffer);

        var sessionPerf = MetricsApi.GetPerformanceSession();
        ref readonly var session = ref sessionPerf.Session;
        ref readonly var baseLine = ref sessionPerf.Baseline;
        var hasBaseLine = sessionPerf.HasBaseline;

        // History
        ImGui.Dummy(new Vector2(0, 4));
        ImGui.SeparatorText("Session vs Last Run");
        
        if(MetricsApi.HasWarmup) ImGui.TextColored(Color4.Green, "Active");
        else ImGui.TextColored(Color4.Cyan, "Warmup");

        MetricHistory(ref za, "Avg:", session.AvgMs, baseLine.AvgMs, hasBaseLine, format: "F3", suffix: "ms",
            space: 55);
        MetricHistory(ref za, "Max:", session.MaxMs, baseLine.MaxMs, hasBaseLine, format: "F3", suffix: "ms",
            space: 55);

        MetricHistory(ref za, "Alloc:", session.AllocatedMb, baseLine.AllocatedMb, hasBaseLine, format: "F0",
            suffix: "MB", space: 55);
        MetricHistory(ref za, "Rate:", allocMbPerSec, session.MaxAllocRate, true, format: "F3", suffix: "MB/s",
            space: 55);

        ImGui.Dummy(new Vector2(0, 4));

        float width = ImGui.GetContentRegionAvail().X;
        float btnWidth = (width - ImGui.GetStyle().ItemSpacing.X) * 0.5f;

        if (ImGui.Button("Reset Session", new Vector2(btnWidth, 0)))
            sessionPerf.ClearCurrent();

        ImGui.SameLine();
        if (ImGui.Button("Set Baseline", new Vector2(btnWidth, 0)))
        {
            sessionPerf.Baseline = sessionPerf.Session;
            sessionPerf.ClearCurrent();

        }

    }
}