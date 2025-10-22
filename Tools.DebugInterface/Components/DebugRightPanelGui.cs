using System.Numerics;
using ImGuiNET;
using Tools.DebugInterface.Data;
using static Tools.DebugInterface.Components.CommonComponents;

namespace Tools.DebugInterface.Components;

internal sealed class DebugRightPanelGui(DebugDataContainer data)
{
    public void DrawRight(int width)
    {
        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(new Vector2(vp.WorkPos.X + vp.WorkSize.X, vp.WorkPos.Y),
            ImGuiCond.Always, new Vector2(1f, 0f));
        ImGui.SetNextWindowSize(new Vector2(width, 0f));

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f));
        ImGui.SetNextWindowBgAlpha(0.95f);

        var flags =
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        if (ImGui.Begin("##RightSidebar", flags))
        {
            DrawCpuMetrics();
            ImGui.Dummy(new Vector2(0, 6));
            DrawGcMetrics();
            ImGui.Dummy(new Vector2(0, 6));
            DrawGpuMetrics();
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private void DrawCpuMetrics()
    {
        DrawSectionHeader("CPU Metrics");
        MetricLine(data.FrameMetrics.FrameIndex);
        MetricLine(data.FrameMetrics.Fps);
        MetricLine(data.FrameMetrics.Alpha);
    }

    private void DrawGcMetrics()
    {
        DrawSectionHeader("GC / Memory");
        MetricLine(data.FrameMetrics.Allocated);
    }

    private void DrawGpuMetrics()
    {
        DrawSectionHeader("GPU Metrics");
        MetricLine(data.FrameMetrics.TriangleCount);
        MetricLine(data.FrameMetrics.DrawCalls);
    }
}