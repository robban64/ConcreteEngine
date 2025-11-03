#region

using System.Numerics;
using Core.DebugTools.Data;
using ImGuiNET;
using static Core.DebugTools.Components.CommonComponents;

#endregion

namespace Core.DebugTools.Components;

internal sealed class DebugRightPanelGui(MetricReport data)
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
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private void DrawCpuMetrics()
    {
        DrawSectionHeader("Frame Metrics");
        TextIfNotNull(data.FrameMetrics.FrameIndex);
        TextIfNotNull(data.FrameMetrics.Fps);
        TextIfNotNull(data.FrameMetrics.Alpha);
        TextIfNotNull(data.FrameMetrics.DrawCalls);
        TextIfNotNull(data.FrameMetrics.TriangleCount);
        TextIfNotNull(data.FrameMetrics.Passes);
    }

    private void DrawGcMetrics()
    {
        DrawSectionHeader("GC / Memory");
        TextIfNotNull(data.MemoryMetrics);
    }
}