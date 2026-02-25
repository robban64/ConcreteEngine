using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Metrics;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.UI.GuiMetrics;

namespace ConcreteEngine.Editor.Panels.Metrics;

internal static class DrawSystemMetrics
{
    public static void DrawFrameMeta(FrameContext ctx)
    {
        scoped ref readonly var frameInfo = ref MetricsApi.Provider<FrameMeta>.Data;
        scoped ref readonly var gpuMeta = ref MetricsApi.Provider<GpuFrameMetaBundle>.Data;

        // Frame Info
        ImGui.SeparatorText("Frame Info"u8);
        MetricText(ctx, "Frame:", frameInfo.FrameId);
        MetricText(ctx, "FPS:", frameInfo.Fps, format: "F2");
        MetricText(ctx, "Alpha:", frameInfo.Alpha, format: "F2", suffix: "ms");


        // Render Frame 
        ImGui.SeparatorText("Render Info"u8);
        MetricText(ctx, "Draws:", gpuMeta.Frame.Draws);
        MetricText(ctx, "Tris:", gpuMeta.Frame.Tris);
    }

    public static void DrawMetrics(FrameContext ctx)
    {
        scoped ref readonly var metric = ref MetricsApi.Provider<PerformanceMetric>.Data;

        // Frame Metric
        ImGui.SeparatorText("Frame Metric"u8);
        MetricText(ctx, "Avg:", metric.AvgMs, format: "F4", suffix: "ms");
        MetricText(ctx, "Max:", metric.MaxMs, format: "F4", suffix: "ms");
        MetricText(ctx, "Min:", metric.MinMs, format: "F4", suffix: "ms");
        MetricText(ctx, "Load:", metric.Load, format: "F4", suffix: "ms");

        ImGui.Dummy(new Vector2(0, 2));

        // Gc Metric
        ImGui.SeparatorText("GC Metric"u8);
        MetricText(ctx, "Allocated:", metric.AllocatedMb, suffix: "MB", space: 70);
        MetricText(ctx, "AllocRate:", metric.AllocMbPerSec, format: "F4", suffix: "MB/s", space: 70);

        var gc = metric.Gc;

        ImGui.TextUnformatted(
            ref ctx.Sw.Append("Generation: "u8).Append("("u8)
                .Append(gc.Gen0).Append(", "u8)
                .Append(gc.Gen1).Append(", "u8)
                .Append(gc.Gen2).Append(")"u8).End()
        );


        ImGui.TextUnformatted("GcActivity: "u8);
        ImGui.SameLine();
        switch (metric.GcActivity)
        {
            case GcActivity.None:
                ImGui.TextUnformatted("Idle"u8);
                break;
            case GcActivity.Minor:
                ImGui.TextColored(Color4.Yellow, "Minor"u8);
                break;
            case GcActivity.Major:
                ImGui.TextColored(Color4.Red, "Major"u8);
                break;
        }
    }

    public static void DrawSession(FrameContext ctx, float allocMbPerSec)
    {
        var sessionPerf = MetricsApi.GetPerformanceSession();
        var hasBaseLine = sessionPerf.HasBaseline;

        // History
        ImGui.Dummy(new Vector2(0, 4));
        ImGui.SeparatorText("Session vs Last Run"u8);

        if (MetricsApi.HasWarmup) ImGui.TextColored(Color4.Green, "Active"u8);
        else ImGui.TextColored(Color4.Cyan, "Warmup"u8);

        scoped ref readonly var session = ref sessionPerf.Session;
        scoped ref readonly var baseLine = ref sessionPerf.Baseline;

        MetricHistory(ctx, "Avg:", session.AvgMs, baseLine.AvgMs, hasBaseLine, format: "F3", suffix: "ms",
            space: 55);
        MetricHistory(ctx, "Max:", session.MaxMs, baseLine.MaxMs, hasBaseLine, format: "F3", suffix: "ms",
            space: 55);

        MetricHistory(ctx, "Alloc:", session.AllocatedMb, baseLine.AllocatedMb, hasBaseLine, format: "F0",
            suffix: "MB", space: 55);
        MetricHistory(ctx, "Rate:", allocMbPerSec, session.MaxAllocRate, true, format: "F3", suffix: "MB/s",
            space: 55);
    }

    public static void DrawFooter()
    {
        var sessionPerf = MetricsApi.GetPerformanceSession();

        var width = ImGui.GetContentRegionAvail().X;
        var btnWidth = (width - ImGui.GetStyle().ItemSpacing.X) * 0.5f;

        ImGui.Dummy(new Vector2(0, 4));

        if (ImGui.Button("Reset Session"u8, new Vector2(btnWidth, 0)))
            sessionPerf.ClearCurrent();

        ImGui.SameLine();
        if (ImGui.Button("Set Baseline"u8, new Vector2(btnWidth, 0)))
        {
            sessionPerf.Baseline = sessionPerf.Session;
            sessionPerf.ClearCurrent();
        }
    }
}