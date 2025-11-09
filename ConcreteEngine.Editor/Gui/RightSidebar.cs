#region

using System.Numerics;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui.Components;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;
using static ConcreteEngine.Editor.Utils.GuiUtils;

#endregion

namespace ConcreteEngine.Editor.Gui;

internal static class RightSidebar
{
    private static bool _focus = false;
    private static bool _prevFocus = false;

    public static void Draw(int width, int offset)
    {
        var viewState = StateCtx.ViewState;
        
        if (!viewState.IsMetricState && viewState.RightSidebar == RightSidebarMode.Default) return;

        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse;

        var vp = ImGui.GetMainViewport();
        var vpSize = vp.WorkSize;

        var height = viewState.IsEmptyViewMode ? 0 : vpSize.Y - offset;
        height = viewState.RightSidebar != RightSidebarMode.Default ? height : 0;


        ImGui.SetNextWindowPos(new Vector2(vpSize.X - width, offset));
        ImGui.SetNextWindowSize(new Vector2(width, height));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (ImGui.Begin("##RightSidebar", flags))
        {
            _focus = ImGui.IsWindowFocused(ImGuiFocusedFlags.None | ImGuiFocusedFlags.ChildWindows);

            if (_focus && !_prevFocus)
            {
                EditorService.OnFetchUpdateCameraData();
            }
            else if (!_focus && _prevFocus)
            {
            }

            switch (viewState.EditorMode)
            {
                case EditorViewMode.Metrics:
                    DrawCpuMetrics();
                    ImGui.Dummy(new Vector2(0, 6));
                    DrawGcMetrics();
                    break;
                case EditorViewMode.Editor:
                    DrawEditor();
                    break;
            }
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
        _prevFocus = _focus;
    }

    private static void DrawEditor()
    {
        switch (StateCtx.ViewState.RightSidebar)
        {
            case RightSidebarMode.Camera:
                CameraPropertyComponent.Draw();
                break;
            case RightSidebarMode.Light:
                break;
            case RightSidebarMode.Sky:
                break;
            case RightSidebarMode.Terrain:
                break;
            case RightSidebarMode.Default:
            default:
                break;
        }
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