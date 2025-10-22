using System.Numerics;
using ImGuiNET;
using Tools.DebugInterface.Data;

namespace Tools.DebugInterface.Components;

internal sealed class DebugRightPanelGui(DebugDataContainer data)
{
    public void DrawRight(int width)
    {
        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(new Vector2(vp.WorkPos.X + vp.WorkSize.X, vp.WorkPos.Y),
            ImGuiCond.Always, new Vector2(1f, 0f));
        ImGui.SetNextWindowSize(new Vector2(width, 0f));

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 8f));
        ImGui.Begin("##RightSidebar",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus);

        DrawCpuMetrics();
        DrawGcMetrics();
        DrawGpuMetrics();

        ImGui.End();
        ImGui.PopStyleVar();
    }

    private void DrawCpuMetrics()
    {
        ImGui.TextUnformatted("CPU Metrics");
        ImGui.Separator();
        ImGui.TextUnformatted(data.FrameMetrics.FrameIndex);
        ImGui.TextUnformatted(data.FrameMetrics.Fps);
        ImGui.TextUnformatted(data.FrameMetrics.Alpha);
        ImGui.Separator();
    }
    
    private void DrawGcMetrics()
    {
        ImGui.TextUnformatted("GC / Memory Metrics");
        ImGui.Separator();
        ImGui.TextUnformatted(data.FrameMetrics.Allocated);
        ImGui.Separator();
    }

    private void DrawGpuMetrics()
    {
        ImGui.TextUnformatted("GPU Metrics");
        ImGui.Separator();
        ImGui.TextUnformatted(data.FrameMetrics.TriangleCount);
        ImGui.TextUnformatted(data.FrameMetrics.DrawCalls);
        ImGui.Separator();
    }
    
    private static string Format(float value) => value.ToString("0.00");

}