using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Theme.GuiMetrics;

namespace ConcreteEngine.Editor.Panels.Metrics;

internal static class DrawSystemMetrics
{
    public static void DrawFrameMeta(FrameContext ctx)
    {
        var it = MetricSystem.Instance;
        // Frame Info
        ImGui.SeparatorText("Frame Info"u8);
        MetricText(ctx, "Frame:", it.FrameMeta.FrameId);

        ImGui.TextUnformatted("FPS:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(ref ctx.Sw.Append(it.FrameMeta.Fps, "F2").Append(" (").Append(it.FrameMeta.Alpha, "F2")
            .Append("ms)").End());


        // Render Frame 
        ref readonly var gpu = ref it.GpuFrameMeta;
        ImGui.SeparatorText("Render Info"u8);
        MetricText(ctx, "Draws:", gpu.Frame.Draws);
        MetricText(ctx, "Tris:", gpu.Frame.Tris);
        ImGui.Spacing();
        MetricText(ctx, "VBO Uploaded:", gpu.Buffer.MeshBufferBytes, space: 0);
        MetricText(ctx, "UBO Uploaded:", gpu.Buffer.UniformBufferBytes, space: 0);
    }

    public static void DrawPerformanceMetrics(FrameContext ctx)
    {
        ref readonly var it = ref MetricSystem.Instance.Metric;

        // Frame Metric
        ImGui.SeparatorText("Frame Metric"u8);
        MetricText(ctx, "Avg:", it.AvgMs, format: "F4", suffix: "ms");
        MetricText(ctx, "Max:", it.MaxMs, format: "F4", suffix: "ms");
        MetricText(ctx, "Min:", it.MinMs, format: "F4", suffix: "ms");
        //MetricText(ctx, "Load:", metric.Load, format: "F4", suffix: "ms");

        ImGui.Dummy(new Vector2(0, 2));

        // Gc Metric
        ImGui.SeparatorText("Runtime Metric"u8);
        MetricText(ctx, "Compiled IL:", it.CompiledILKb, suffix: "KB", space: 80);
        MetricText(ctx, "Allocated:", it.AllocatedMb, suffix: "MB", space: 70);
        MetricText(ctx, "AllocRate:", it.AllocMbPerSec, format: "F4", suffix: "MB/s", space: 70);

        var status = it.GcActivity switch
        {
            GcActivity.None => "Idle",
            GcActivity.Minor => "Minor",
            GcActivity.Major => "Major",
            _ => "-"
        };
        ImGui.TextUnformatted(ref ctx.Sw.Append("Status: ["u8).Append(status).Append(']').End());
        ImGui.SameLine();
        ImGui.TextUnformatted(
            ref ctx.Sw.Append("Gen: "u8).Append('[')
                .Append(it.Gc.Gen0).Append(", "u8)
                .Append(it.Gc.Gen1).Append(", "u8)
                .Append(it.Gc.Gen2).Append(']').End()
        );
    }

    // TODO
    /*
    public static void DrawSession(FrameContext ctx, float allocMbPerSec)
    {
        //var sessionPerf = MetricSystem.Instance.PerfSession;

        var hasBaseLine = false; //sessionPerf.HasBaseline;

        // History
        ImGui.SeparatorText("Current vs Last"u8);
        if (MetricSystem.Instance.IsWarmup) ImGui.TextColored(Color4.Cyan, "Warmup"u8);
        else ImGui.TextColored(Color4.Green, "Active"u8);

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
    }*/
}