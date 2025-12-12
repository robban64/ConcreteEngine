#region

using System.Numerics;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;
using static ConcreteEngine.Editor.Utils.GuiUtils;

#endregion

namespace ConcreteEngine.Editor.Metrics;

internal static class SystemMetricsGui
{
    private const int WindowPaddingX = 12;

    public static void Draw()
    {
        const ImGuiChildFlags flags = ImGuiChildFlags.AlwaysUseWindowPadding;
        var size = new Vector2(GuiTheme.RightSidebarWidth - WindowPaddingX, 0);

        if (!ImGui.BeginChild("##system-metrics-gui", size, flags)) return;

        DrawCpuMetrics();
        ImGui.Dummy(new Vector2(0, 6));
        DrawGcMetrics();

        ImGui.EndChild();
    }

    private static void DrawCpuMetrics()
    {
        var data = MetricsApi.TextData;
        ImGui.SeparatorText("Frame Metrics");
        TextIfNotNull(data.FrameMetrics.FrameIndex);
        TextIfNotNull(data.FrameMetrics.Fps);
        TextIfNotNull(data.FrameMetrics.Alpha);
        TextIfNotNull(data.FrameMetrics.DrawCalls);
        TextIfNotNull(data.FrameMetrics.TriangleCount);
        TextIfNotNull(data.FrameMetrics.Passes);
    }

    private static void DrawGcMetrics()
    {
        var data = MetricsApi.TextData;
        ImGui.SeparatorText("GC / Memory");
        TextIfNotNull(data.MemoryMetrics);
    }
}