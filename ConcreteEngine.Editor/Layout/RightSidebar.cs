#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor.Layout;

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

        ImGui.SetNextWindowPos(new Vector2(vpSize.X - width, offset));
        ImGui.SetNextWindowSize(new Vector2(width, height));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (ImGui.Begin("##RightSidebar", flags))
        {
            _focus = ImGui.IsWindowFocused(ImGuiFocusedFlags.None | ImGuiFocusedFlags.ChildWindows);
            //if(_focus &&  !_prevFocus) EditorModelManager.CameraState.TriggerEvent(EventKey.SelectionUpdated);

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
            case RightSidebarMode.Default:
            case RightSidebarMode.Camera: CameraComponent.Draw(); break;
            case RightSidebarMode.World: WorldParamsComponent.Draw(); break;
            case RightSidebarMode.Sky: break;
            case RightSidebarMode.Terrain: break;
            default: break;
        }
    }
}