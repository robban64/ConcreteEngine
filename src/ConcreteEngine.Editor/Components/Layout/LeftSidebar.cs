using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components.Layout;

internal static class LeftSidebar
{
    public static int Width;
    public static int Height;


    public static void Draw(ModelStateContext ctx)
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                                       ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        var vp = ImGui.GetMainViewport();
        var mode = StateManager.ModeState;

        var height = !mode.IsActive ? 0 : vp.WorkSize.Y - GuiTheme.TopbarHeight;
        height = mode.LeftSidebar != LeftSidebarMode.Default ? height : 0;


        ImGui.SetNextWindowPos(new Vector2(0, GuiTheme.TopbarHeight));
        ImGui.SetNextWindowSize(new Vector2(Width, height));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (!ImGui.Begin("##LeftSidebar"u8, flags))
        {
            ImGui.End();
            return;
        }

        if (mode.LeftSidebar == LeftSidebarMode.Metrics)
        {
            ctx.DrawLeft();
            ImGui.End();
            return;
        }
        
        var isAssets = mode.LeftSidebar == LeftSidebarMode.Assets;
        var isScene = mode.LeftSidebar == LeftSidebarMode.Scene;

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));

        if (ImGui.BeginTabBar("##left_panel_tabs"u8, ImGuiTabBarFlags.FittingPolicyShrink))
        {
            if (isAssets) ImGui.PushStyleColor(ImGuiCol.Tab, GuiTheme.SelectedColor);
            if (ImGui.TabItemButton("Asset##asset-tab-btn"u8))
                StateManager.SetLeftSidebarState(LeftSidebarMode.Assets);
            if (isAssets) ImGui.PopStyleColor();

            if (isScene) ImGui.PushStyleColor(ImGuiCol.Tab, GuiTheme.SelectedColor);
            if (ImGui.TabItemButton("Scene##scene-tab-btn"u8))
                StateManager.SetLeftSidebarState(LeftSidebarMode.Scene);
            if (isScene) ImGui.PopStyleColor();

            ImGui.EndTabBar();
        }

        ctx.DrawLeft();

        ImGui.PopStyleVar();
        ImGui.End();
    }

    private static void DrawMetrics()
    {
        if (ImGui.BeginChild("##left-sidebar-metrics"u8, new Vector2(0),
                ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.AutoResizeY))
        {
            if (MetricsApi.Store.Assets is not null)
                AssetStoreMetricsGui.DrawAssetStoreMetrics();

            ImGui.Dummy(new Vector2(0, 6));

            if (MetricsApi.Store.Gfx is not null)
                GfxStoreMetricsGui.DrawGfxStoreMetrics();

            ImGui.EndChild();
        }
    }

    private static void DrawEditor()
    {
        var state = StateManager.ModeState.LeftSidebar;

        var height = state == LeftSidebarMode.Default ? 24 : 0;
        if (!ImGui.BeginChild("##left-sidebar-editor-header"u8, new Vector2(0, height), ImGuiChildFlags.None))
        {
            ImGui.EndChild();
            return;
        }

        var isAssets = state == LeftSidebarMode.Assets;
        var isScene = state == LeftSidebarMode.Scene;

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));

        if (ImGui.BeginTabBar("##left_panel_tabs"u8, ImGuiTabBarFlags.FittingPolicyShrink))
        {
            if (isAssets) ImGui.PushStyleColor(ImGuiCol.Tab, GuiTheme.SelectedColor);
            if (ImGui.TabItemButton("Asset##asset-tab-btn"u8))
                StateManager.SetLeftSidebarState(LeftSidebarMode.Assets);
            if (isAssets) ImGui.PopStyleColor();

            if (isScene) ImGui.PushStyleColor(ImGuiCol.Tab, GuiTheme.SelectedColor);
            if (ImGui.TabItemButton("Scene##scene-tab-btn"u8))
                StateManager.SetLeftSidebarState(LeftSidebarMode.Scene);
            if (isScene) ImGui.PopStyleColor();

            ImGui.EndTabBar();
        }

        ImGui.PopStyleVar();
        ImGui.EndChild();
    }
}