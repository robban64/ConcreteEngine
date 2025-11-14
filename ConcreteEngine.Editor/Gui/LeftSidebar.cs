#region

using System.Numerics;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui.Components;
using ConcreteEngine.Editor.Gui.Metrics;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor.Gui;

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
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 0));
        if (ImGui.BeginChild("##left-sidebar-editor-header", new Vector2(0),
                ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.AutoResizeY))
        {
            ImGui.PopStyleVar();

            DrawModeSelector();

            if (StateContext.ModeState.LeftSidebar == LeftSidebarMode.Assets)
            {
                AssetsComponent.DrawSubHeader();
                AssetsComponent.Draw();
            }

            ImGui.EndChild();
        }

        if (StateContext.ModeState.LeftSidebar == LeftSidebarMode.Entities)
            EntitiesComponent.Draw();
    }

    private static void DrawModeSelector()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));

        if (ImGui.BeginTabBar("left_panel_tabs", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem("Assets"))
            {
                StateContext.SetLeftSidebarState(LeftSidebarMode.Assets);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Entities"))
            {
                StateContext.SetLeftSidebarState(LeftSidebarMode.Entities);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.PopStyleVar(1);
    }
}