using System.Numerics;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

namespace ConcreteEngine.Editor.Components.Layout;

internal static class LeftSidebar
{
    public static void Draw(int width, int offset)
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                                       ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        var vp = ImGui.GetMainViewport();

        var height = StateContext.ModeState.IsEmptyViewMode ? 0 : vp.WorkSize.Y - offset;
        height = StateContext.ModeState.LeftSidebar != LeftSidebarMode.Default ? height : 0;


        ImGui.SetNextWindowPos(new Vector2(0, offset));
        ImGui.SetNextWindowSize(new Vector2(width, height));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (ImGui.Begin("##LeftSidebar", flags))
        {
            switch (StateContext.ModeState.Mode)
            {
                case ViewMode.Metrics: DrawMetrics(); break;
                case ViewMode.Editor: DrawEditor(); break;
            }
        }

        ImGui.End();
    }

    private static void DrawMetrics()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 0));
        if (ImGui.BeginChild("##left-sidebar-metrics", new Vector2(0),
                ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.AutoResizeY))
        {
            ImGui.Dummy(new Vector2(0, 6));
            AssetStoreMetricsGui.DrawAssetStoreMetrics();
            ImGui.Dummy(new Vector2(0, 6));
            GfxStoreMetricsGui.DrawGfxStoreMetrics();
            ImGui.EndChild();
        }

        ImGui.PopStyleVar();
    }

    private static void DrawEditor()
    {
        var state = StateContext.ModeState.LeftSidebar;
        var isAssets = state == LeftSidebarMode.Assets;
        var isEntities = state == LeftSidebarMode.Entities;
        var isScene = state == LeftSidebarMode.Scene;

        var height = state == LeftSidebarMode.Default ? 24 : 0;
        if (!ImGui.BeginChild("##left-sidebar-editor-header", new Vector2(0, height), ImGuiChildFlags.None))
            return;

        ImGui.PopStyleVar();

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));

        if (ImGui.BeginTabBar("##left_panel_tabs", ImGuiTabBarFlags.None))
        {
            if (isAssets) ImGui.PushStyleColor(ImGuiCol.Tab, GuiTheme.SelectedColor);
            if (ImGui.TabItemButton("Asset##asset-tab-btn"))
                StateContext.SetLeftSidebarState(LeftSidebarMode.Assets);
            if (isAssets) ImGui.PopStyleColor();

            if (isEntities) ImGui.PushStyleColor(ImGuiCol.Tab, GuiTheme.SelectedColor);
            if (ImGui.TabItemButton("Entity##entities-tab-btn"))
                StateContext.SetLeftSidebarState(LeftSidebarMode.Entities);
            if (isEntities) ImGui.PopStyleColor();

            if (isScene) ImGui.PushStyleColor(ImGuiCol.Tab, GuiTheme.SelectedColor);
            if (ImGui.TabItemButton("Scene##scene-tab-btn"))
                StateContext.SetLeftSidebarState(LeftSidebarMode.Scene);
            if (isScene) ImGui.PopStyleColor();

            switch (state)
            {
                case LeftSidebarMode.Assets: AssetsComponent.Draw(); break;
                case LeftSidebarMode.Entities: EntitiesComponent.Draw(); break;
                case LeftSidebarMode.Scene: SceneListComponent.Draw(); break;
                case LeftSidebarMode.Default:
                default: break;
            }

            ImGui.EndTabBar();
        }

        ImGui.PopStyleVar(1);

        ImGui.EndChild();
    }
}