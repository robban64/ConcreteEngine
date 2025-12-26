using System.Numerics;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;
using ZaString.Core;
using ZaString.Extensions;
using static ConcreteEngine.Editor.Utils.GuiUtils;

namespace ConcreteEngine.Editor.Metrics;

internal static class SystemMetricsGui
{
    private const int WindowPaddingX = 12;

    public static void Draw()
    {
        const ImGuiChildFlags flags = ImGuiChildFlags.AlwaysUseWindowPadding;
        var size = new Vector2(GuiTheme.RightSidebarWidth - WindowPaddingX, 0);

        if (!ImGui.BeginChild("##system-metrics-gui", size, flags)) return;

        DrawFrameMetrics();
        ImGui.Dummy(new Vector2(0, 6));
        DrawGcMetrics();

        ImGui.EndChild();
    }

    private static void DrawFrameMetrics()
    {
        var data = MetricsApi.TextData;
        ImGui.SeparatorText("Frame Metrics");

        Span<char> buffer = stackalloc char[32];
        var za = ZaSpanStringBuilder.Create(buffer);
        
        ref readonly var f = ref MetricsApi.FrameMeta.Frame;
        var r = MetricStore.FrameMeta.RenderFrame;
        MetricText(ref za, "Frame:", f.FrameId,  suffix: "ms");
        MetricText(ref za, "FPS:", f.Fps, format: "F2");
        MetricText(ref za, "Alpha:", f.Alpha, format: "F2", suffix: "ms");
        MetricText(ref za, "Draws:", r.Draws);
        MetricText(ref za, "Tris:", r.Tris);

    }

    private static void DrawGcMetrics()
    {
        var data = MetricsApi.TextData;
        ImGui.SeparatorText("GC / Memory");
        TextIfNotNull(data.MemoryMetrics);
    }
}