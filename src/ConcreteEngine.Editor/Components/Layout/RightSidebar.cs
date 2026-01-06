using System.Numerics;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components.Layout;

internal static class RightSidebar
{
    public static int Width;

    public static void Draw(float delta)
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus |
                                       ImGuiWindowFlags.NoCollapse;

        var viewState = StateContext.ModeState;

        var vp = ImGui.GetMainViewport();
        var vpSize = vp.WorkSize;

        var height = viewState.IsEmptyViewMode ? 0 : vpSize.Y - GuiTheme.TopbarHeight;

        ImGui.SetNextWindowPos(new Vector2(vpSize.X - Width, GuiTheme.TopbarHeight));
        ImGui.SetNextWindowSize(new Vector2(Width, height));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (!ImGui.Begin("##RightSidebar"u8, flags))
        {
            ImGui.End();
            return;
        }

        if (viewState.IsMetricState)
        {
            SystemMetricsGui.Draw(delta);
            ImGui.End();
            return;
        }

        switch (StateContext.ModeState.RightSidebar)
        {
            case RightSidebarMode.Default:
            case RightSidebarMode.Camera: CameraComponent.Draw(); break;
            case RightSidebarMode.World: WorldParamsComponent.Draw(); break;
            case RightSidebarMode.Property: EntitiesComponent.DrawProperties(); break;
            case RightSidebarMode.SceneObject: SceneObjectComponent.Draw(); break;
            case RightSidebarMode.Sky:
            case RightSidebarMode.Terrain:
            default: break;
        }

        ImGui.End();
    }

}