using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.UI.GuiMetrics;

namespace ConcreteEngine.Editor.Panels.Metrics;

internal static class DrawSystemMetrics
{
    public static void DrawFrameMeta(FrameContext ctx)
    {
        var it = MetricSystem.Instance;

        // Frame Info
        ImGui.SeparatorText("Frame Info"u8);
        MetricText(ctx, "Frame:", it.FrameMeta.FrameId);
        MetricText(ctx, "FPS:", it.FrameMeta.Fps, format: "F2");
        MetricText(ctx, "Alpha:", it.FrameMeta.Alpha, format: "F2", suffix: "ms");


        // Render Frame 
        ImGui.SeparatorText("Render Info"u8);
        MetricText(ctx, "Draws:", it.GpuFrameMeta.Frame.Draws);
        MetricText(ctx, "Tris:", it.GpuFrameMeta.Frame.Tris);

    }

    public static void DrawPerformanceMetrics(FrameContext ctx)
    {
        var it = MetricSystem.Instance;

        // Frame Metric
        ImGui.SeparatorText("Frame Metric"u8);
        MetricText(ctx, "Avg:", it.FrameMetric.AvgMs, format: "F4", suffix: "ms");
        MetricText(ctx, "Max:", it.FrameMetric.MaxMs, format: "F4", suffix: "ms");
        MetricText(ctx, "Min:", it.FrameMetric.MinMs, format: "F4", suffix: "ms");
        //MetricText(ctx, "Load:", metric.Load, format: "F4", suffix: "ms");

        ImGui.Dummy(new Vector2(0, 2));

        // Gc Metric
        ref readonly var runtimeMetric = ref it.RuntimeMetric ;
        ImGui.SeparatorText("Runtime Metric"u8);
        MetricText(ctx, "IL Bytes:", runtimeMetric.CompiledILKb, suffix: "KB", space: 70);
        MetricText(ctx, "IL Delta:", runtimeMetric.CompiledILRateKb, suffix: "KB/s", format: "F4", space: 70);

        ImGui.SeparatorText("GC Metric"u8);
        MetricText(ctx, "Allocated:", runtimeMetric.AllocatedMb, suffix: "MB", space: 70);
        MetricText(ctx, "AllocRate:", runtimeMetric.AllocMbPerSec, format: "F4", suffix: "MB/s", space: 70);

        ImGui.TextUnformatted("Status: "u8);
        ImGui.SameLine();
        switch (runtimeMetric.GcActivity)
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
        ImGui.SameLine();
        ImGui.TextUnformatted(
            ref ctx.Sw.Append("Gen: "u8).Append('[')
                .Append(it.GcSample.Gen0).Append(", "u8)
                .Append(it.GcSample.Gen1).Append(", "u8)
                .Append(it.GcSample.Gen2).Append(']').End()
        );


    }

    public static void DrawSession(FrameContext ctx, float allocMbPerSec)
    {
        //var sessionPerf = MetricSystem.Instance.PerfSession;

        var hasBaseLine = false; //sessionPerf.HasBaseline;

        // History
        ImGui.SeparatorText("Current vs Last"u8);
        if (MetricSystem.Instance.IsWarmup) ImGui.TextColored(Color4.Green, "Active"u8);
        else ImGui.TextColored(Color4.Cyan, "Warmup"u8);

        ref readonly var session = ref MetricSystem.Instance.FrameMetric;
        ref readonly var baseLine = ref MetricSystem.Instance.FrameMetric;

        var AllocatedMb = MetricSystem.Instance.RuntimeMetric.AllocatedMb;
        var AllocRate = MetricSystem.Instance.RuntimeMetric.AllocMbPerSec;


        MetricHistory(ctx, "Avg:", session.AvgMs, baseLine.AvgMs, hasBaseLine, format: "F3", suffix: "ms",
            space: 55);
        MetricHistory(ctx, "Max:", session.MaxMs, baseLine.MaxMs, hasBaseLine, format: "F3", suffix: "ms",
            space: 55);

        MetricHistory(ctx, "Alloc:", AllocatedMb, AllocatedMb, hasBaseLine, format: "F0",
            suffix: "MB", space: 55);
        MetricHistory(ctx, "Rate:", allocMbPerSec, AllocRate, true, format: "F3", suffix: "MB/s",
            space: 55);
    }

    public static void DrawFooter()
    {
        // var sessionPerf = MetricSystem.Instance.PerfSession;

        var btnWidth = GuiLayout.GetRowWidthForItems(2);

        if (ImGui.Button("Reset Session"u8, new Vector2(btnWidth, 0))) { }
        //    sessionPerf.ClearCurrent();

        ImGui.SameLine();
        if (ImGui.Button("Set Baseline"u8, new Vector2(btnWidth, 0)))
        {
            //  sessionPerf.Baseline = sessionPerf.Session;
            //  sessionPerf.ClearCurrent();
        }
    }
}