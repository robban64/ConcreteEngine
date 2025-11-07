using System.Numerics;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui.Metrics;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

namespace ConcreteEngine.Editor.Gui;

internal sealed class LeftSidebar
{
    private readonly EditorStateContext _ctx;

    private readonly AssetStoreGui _assetStoreGui;
    private readonly EntityListGui _entityListGui;
    
    public LeftSidebar( EditorStateContext ctx)
    {
        _ctx = ctx;
        _assetStoreGui = new AssetStoreGui(ctx);
        _entityListGui = new EntityListGui(ctx);
    }


    public void Draw(int width, int offset)
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                                       ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;
        
        var vp = ImGui.GetMainViewport();
        var height = _ctx.ViewMode == EditorViewMode.None ? 0 : vp.WorkSize.Y - offset;
        height = _ctx.LeftSidebarMode != LeftSidebarMode.None ? height : 0;

        ImGui.SetNextWindowPos(new Vector2(0, offset));
        ImGui.SetNextWindowSize(new Vector2(width, height));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 10f));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (ImGui.Begin("##LeftSidebar", flags))
        {
            switch (_ctx.ViewMode)
            {
                case EditorViewMode.Metrics: DrawMetrics(); break;
                case EditorViewMode.Editor: DrawEditor(); break;
            }
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }
    
    private void DrawMetrics()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 0));
        if(ImGui.BeginChild("##left-sidebar-metrics", new Vector2(0), ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.AutoResizeY))
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
    
    private void DrawEditor()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 0));
        if(ImGui.BeginChild("##left-sidebar-editor-header", new Vector2(0), ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.AutoResizeY))
        {
            ImGui.PopStyleVar();

            DrawModeSelector();

            if (_ctx.LeftSidebarMode == LeftSidebarMode.Assets)
            {
                _assetStoreGui.DrawSubHeader();
                _assetStoreGui.Draw();
            }
            
            ImGui.EndChild();
        }

       if(_ctx.LeftSidebarMode == LeftSidebarMode.Entities)
           _entityListGui.Draw();
        
    }

    private void DrawModeSelector()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));
        ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 0.5f);
        ImGui.PushStyleVar(ImGuiStyleVar.TabBarBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.TabBorderSize, 1);
        ImGui.PushStyleColor(ImGuiCol.TabHovered, GuiTheme.Blue1);
        ImGui.PushStyleColor(ImGuiCol.TabActive, GuiTheme.SelectedColor);
        ImGui.PushStyleColor(ImGuiCol.Tab, GuiTheme.PrimaryColor);


        if (ImGui.BeginTabBar("left_panel_tabs", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem("None"))
            {
                _ctx.SetSidebarMode(LeftSidebarMode.None);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Assets"))
            {
                _ctx.SetSidebarMode(LeftSidebarMode.Assets);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Entities"))
            {
                _ctx.SetSidebarMode(LeftSidebarMode.Entities);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
        ImGui.PopStyleVar(4);
        ImGui.PopStyleColor(3);
    }


}