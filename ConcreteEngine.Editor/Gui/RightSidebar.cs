#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui.Components;
using ConcreteEngine.Editor.Gui.Metrics;
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
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                                       ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        var viewState = StateContext.ModeState;
        if (!viewState.IsMetricState && viewState.RightSidebar == RightSidebarMode.Default) return;

        var vp = ImGui.GetMainViewport();
        var vpSize = vp.WorkSize;

        var height = viewState.IsEmptyViewMode ? 0 : vpSize.Y - offset;
        height = viewState.RightSidebar != RightSidebarMode.Default ? height : 0;

        ImGui.SetNextWindowPos(new Vector2(vpSize.X - width, offset));
        ImGui.SetNextWindowSize(new Vector2(width, height));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (ImGui.Begin("##RightSidebar", flags))
        {
            _focus = ImGui.IsWindowFocused(ImGuiFocusedFlags.None | ImGuiFocusedFlags.ChildWindows);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 0));
            switch (viewState.EditorMode)
            {
                case EditorViewMode.Metrics: SystemMetricsGui.Draw(); break;
                case EditorViewMode.Editor: DrawEditor(); break;
            }

            ImGui.PopStyleVar();
        }

        ImGui.End();
        _prevFocus = _focus;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawEditor()
    {
        switch (StateContext.ModeState.RightSidebar)
        {
            case RightSidebarMode.Camera: CameraPropertyComponent.Draw(); break;
            case RightSidebarMode.World: WorldParamsComponent.Draw(); break;
            case RightSidebarMode.Sky: break;
            case RightSidebarMode.Terrain: break;
            case RightSidebarMode.Default:
            default: break;
        }
    }

}