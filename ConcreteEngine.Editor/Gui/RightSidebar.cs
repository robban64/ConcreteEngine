#region

using System.Numerics;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;
using static ConcreteEngine.Editor.Utils.GuiUtils;

#endregion

namespace ConcreteEngine.Editor.Gui;

internal sealed class RightSidebar
{
    private readonly EditorStateContext _ctx;

    public RightSidebar( EditorStateContext ctx)
    {
        _ctx = ctx;
        
    }
    
    private bool _focus = false;

    private bool _prevFocus = false;

    public void Draw(int width, int offset)
    {
        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse ;

        var vp = ImGui.GetMainViewport();
        var vpSize = vp.WorkSize;
        
        var height = _ctx.ViewMode == EditorViewMode.None ? 0 : vpSize.Y - offset;
        height = _ctx.LeftSidebarMode != LeftSidebarMode.None ? height : 0;
        
        
        ImGui.SetNextWindowPos(new Vector2(vpSize.X - width, offset));
        ImGui.SetNextWindowSize(new Vector2(width, height));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (ImGui.Begin("##RightSidebar", flags))
        {
            _focus = ImGui.IsWindowFocused(ImGuiFocusedFlags.None | ImGuiFocusedFlags.ChildWindows);

            if (_focus && !_prevFocus)
            {
                _ctx.RefreshCameraData();
            }
            else if (!_focus && _prevFocus)
            {
                
            }
            
            switch (_ctx.ViewMode)
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

    private void DrawEditor()
    {
        switch (_ctx.PropertyMode)
        {
            case RightSidebarMode.None:
                break;
            case RightSidebarMode.Camera:
                CameraPropertyGui.Draw();
                break;
            case RightSidebarMode.Light:
                break;
            case RightSidebarMode.Sky:
                break;
            case RightSidebarMode.Terrain:
                break;
            default:
                throw new ArgumentOutOfRangeException();
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