#region

using System.Numerics;
using Core.DebugTools.Data;
using Core.DebugTools.Definitions;
using Core.DebugTools.Editor;
using Core.DebugTools.Utils;
using ImGuiNET;
using static Core.DebugTools.Utils.GuiUtils;

#endregion

namespace Core.DebugTools.Gui;

internal sealed class RightSidebar
{
    private readonly EditorStateContext _ctx;

    public RightSidebar( EditorStateContext ctx)
    {
        _ctx = ctx;
        
    }

    public void Draw(int width, int offset)
    {
        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        var vp = ImGui.GetMainViewport();
        var vpSize = vp.WorkSize;
        
        var height = _ctx.ViewMode == EditorViewMode.None ? 0 : vpSize.Y - offset;
        height = _ctx.SidebarMode != SidebarEditorMode.None ? height : 0;
        
        
        ImGui.SetNextWindowPos(new Vector2(vpSize.X - width, offset));
        ImGui.SetNextWindowSize(new Vector2(width, height));

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
        var data = MetricsTable.TextData;
        ImGui.SeparatorText("Frame Metrics");
        TextIfNotNull(data.FrameMetrics.FrameIndex);
        TextIfNotNull(data.FrameMetrics.Fps);
        TextIfNotNull(data.FrameMetrics.Alpha);
        TextIfNotNull(data.FrameMetrics.DrawCalls);
        TextIfNotNull(data.FrameMetrics.TriangleCount);
        TextIfNotNull(data.FrameMetrics.Passes);
    }

    private void DrawGcMetrics()
    {
        var data = MetricsTable.TextData;
        ImGui.SeparatorText("GC / Memory");
        TextIfNotNull(data.MemoryMetrics);
    }
}