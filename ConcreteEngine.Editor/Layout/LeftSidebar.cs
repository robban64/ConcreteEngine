#region

using System.Numerics;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor.Layout;

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
            switch (StateContext.ModeState.EditorMode)
            {
                case EditorViewMode.Metrics: DrawMetrics(); break;
                case EditorViewMode.Editor: DrawEditor(); break;
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
            var metrics = MetricsApi.TextData;
            SceneMetricsGui.DrawSceneMetrics(metrics.SceneMetrics);
            ImGui.Dummy(new Vector2(0, 6));
            AssetStoreMetricsGui.DrawAssetStoreMetrics(metrics);
            ImGui.Dummy(new Vector2(0, 6));
            GfxStoreMetricsGui.DrawGfxStoreMetrics(metrics);
            ImGui.EndChild();
        }

        ImGui.PopStyleVar();
    }

    private static void DrawEditor()
    {
        var state = StateContext.ModeState.LeftSidebar;
        var height = state == LeftSidebarMode.Default ? 24 : 0;
        if (ImGui.BeginChild("##left-sidebar-editor-header", new Vector2(0,height)))
        {
            ImGui.PopStyleVar();
            DrawModeSelector();
            ImGui.EndChild();
        }
    }

    private static void DrawModeSelector()
    {
        var state = StateContext.ModeState.LeftSidebar;
        var isAssets = state == LeftSidebarMode.Assets;
        var isEntities = state == LeftSidebarMode.Entities;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));

        if (ImGui.BeginTabBar("##left_panel_tabs", ImGuiTabBarFlags.None))
        {
            if (isAssets) ImGui.PushStyleColor(ImGuiCol.Tab, GuiTheme.SelectedColor);
            if (ImGui.TabItemButton("Assets##asset-tab-btn")) StateContext.SetLeftSidebarState(LeftSidebarMode.Assets);
            if (isAssets) ImGui.PopStyleColor();

            if (isEntities) ImGui.PushStyleColor(ImGuiCol.Tab, GuiTheme.SelectedColor);
            if (ImGui.TabItemButton("Entities##entities-tab-btn"))
                StateContext.SetLeftSidebarState(LeftSidebarMode.Entities);
            if (isEntities) ImGui.PopStyleColor();

            switch (state)
            {
                case LeftSidebarMode.Assets: AssetsComponent.Draw(); break;
                case LeftSidebarMode.Entities: EntitiesComponent.Draw(); break;
                case LeftSidebarMode.Default:
                default: break;
            }

            ImGui.EndTabBar();
        }

        ImGui.PopStyleVar(1);
    }
}