using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;
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

        DrawFrameMetrics(delta);

        ImGui.EndChild();
    }

    private static void TickGcActivity(float delta, GcActivity activity)
    {
        if(_gcActivity == GcActivity.None && activity == GcActivity.None) return;
        
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

    private static void DrawFrameMetrics(float delta)
    {
        var frameInfo = MetricsApi.Provider<FrameMetaBundle>.Record?.Data ?? default;
        var metric = MetricsApi.Provider<PerformanceMetric>.Record?.Data ?? default;
        TickGcActivity(delta, metric.GcActivity);

        Span<char> buffer = stackalloc char[32];
        var za = ZaSpanStringBuilder.Create(buffer);

        // Frame Info
        ImGui.SeparatorText("Frame Info");
        MetricText(ref za, "Frame:", frameInfo.Frame.FrameId);
        MetricText(ref za, "FPS:", frameInfo.Frame.Fps, format: "F2");
        MetricText(ref za, "Alpha:", frameInfo.Frame.Alpha, format: "F2", suffix: "ms");

        // Render Frame 
        ImGui.Separator();
        MetricText(ref za, "Draws:", frameInfo.RenderFrame.Draws);
        MetricText(ref za, "Tris:", frameInfo.RenderFrame.Tris);

        ImGui.Dummy(new Vector2(0, 6));

        // Frame Metric
        ImGui.SeparatorText("Frame Metric");
        MetricText(ref za, "Avg:", metric.AvgMs, format: "F4", suffix: "ms");
        MetricText(ref za, "Max:", metric.MaxMs, format: "F4", suffix: "ms");
        MetricText(ref za, "Min:", metric.MinMs, format: "F4", suffix: "ms");
        MetricText(ref za, "Load:", metric.Load, format: "F4", suffix: "ms");

        // Gc Metric
        ImGui.SeparatorText("GC Metric");
        MetricText(ref za, "Allocated:", metric.AllocatedMb, suffix: "MB", space: 70);
        MetricText(ref za, "AllocRate:", metric.AllocMbPerSec, suffix:"s", format:"F4", space: 70);

        ImGui.TextUnformatted("GcActivity:");
        switch (metric.GcActivity)
        {
            case GcActivity.Minor: ImGui.TextColored(Color4.Yellow.AsVec4(),"Minor"); break;
            case GcActivity.Major: ImGui.TextColored(Color4.Red.AsVec4(),"Major"); break;
        }
    }
}