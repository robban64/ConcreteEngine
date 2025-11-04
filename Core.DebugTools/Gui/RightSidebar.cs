#region

using System.Numerics;
using Core.DebugTools.Data;
using Core.DebugTools.Definitions;
using Core.DebugTools.Utils;
using ImGuiNET;
using static Core.DebugTools.Utils.GuiUtils;

#endregion

namespace Core.DebugTools.Gui;

internal sealed class RightSidebar
{
    private readonly MetricService _metricService;
    private readonly EditorStateContext _ctx;

    public RightSidebar(MetricService metricService, EditorStateContext ctx)
    {
        _metricService = metricService;
        _ctx = ctx;
        
    }

    public void Draw(int width, int offset)
    {
        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        var vp = ImGui.GetMainViewport();
        var vpSize = vp.WorkSize;
        ImGui.SetNextWindowPos(new Vector2(vpSize.X - width, offset));
        ImGui.SetNextWindowSize(new Vector2(width, vpSize.Y - offset));

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (ImGui.Begin("##RightSidebar", flags))
        {
            if (_ctx.ViewMode == EditorViewMode.Metrics)
            {
                DrawCpuMetrics();
                ImGui.Dummy(new Vector2(0, 6));
                DrawGcMetrics();
            }
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private void DrawCpuMetrics()
    {
        var data = _metricService.TextData;
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
        var data = _metricService.TextData;
        DrawSectionHeader("GC / Memory");
        TextIfNotNull(data.MemoryMetrics);
    }
}