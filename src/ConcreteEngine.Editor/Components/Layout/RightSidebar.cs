using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

namespace ConcreteEngine.Editor.Components.Layout;

internal static class RightSidebar
{
    public static void Draw(float delta, int width, int offset)
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus |
                                       ImGuiWindowFlags.NoCollapse;

        var viewState = StateContext.ModeState;

        var vp = ImGui.GetMainViewport();
        var vpSize = vp.WorkSize;

        var height = viewState.IsEmptyViewMode ? 0 : vpSize.Y - offset;

        ImGui.SetNextWindowPos(new Vector2(vpSize.X - width, offset));
        ImGui.SetNextWindowSize(new Vector2(width, height));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6f, 4f));
        if (!ImGui.Begin("##RightSidebar", flags))
        {
            ImGui.PopStyleVar();
            return;
        }
        
        switch (viewState.Mode)
        {
            case ViewMode.Metrics: SystemMetricsGui.Draw(delta); break;
            case ViewMode.Editor: DrawEditor(); break;
            case ViewMode.None: 
            default: break;
        }
        
        ImGui.PopStyleVar();
        ImGui.End();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawEditor()
    {
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
    }
}