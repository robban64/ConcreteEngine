using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components;

internal sealed class Layout(StateContext stateContext)
{
    const int TopBtnWidth = 74;

    private PanelSize _panelSize;
    
    private EnumTabBar<LeftSidebarMode> _leftTabBar = new(2);

    public void SetPanelSize(in PanelSize panelSize) => _panelSize = panelSize;

    public void DrawTop()
    {
        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(vp.WorkPos);
        ImGui.SetNextWindowSize(vp.Size with { Y = GuiTheme.TopbarHeight });
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

        if (ImGui.Begin("##topbar"u8, GuiTheme.TopbarFlags))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));

            // left
            DrawModeSelector();

            // right
            DrawPropertySelector(stateContext);

            ImGui.PopStyleVar(1);
            ImGui.End();
        }

        ImGui.PopStyleVar(1);
    }

    public void DrawRight(ComponentRuntime? comp, FrameContext ctx)
    {
        if (comp == null) return;

        ImGui.SetNextWindowPos(_panelSize.RightPosition);
        ImGui.SetNextWindowSize(_panelSize.RightSize);
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (!ImGui.Begin("##right-sidebar"u8, GuiTheme.SidebarFlags))
        {
            ImGui.End();
            return;
        }

        ImGui.PushID("##right-sidebar-body"u8);
        comp.DrawRight(ref ctx);
        ImGui.PopID();

        ImGui.End();
    }

    public void DrawLeft(ComponentRuntime? comp, FrameContext ctx)
    {
        ImGui.SetNextWindowPos(_panelSize.LeftPosition);
        ImGui.SetNextWindowSize(_panelSize.LeftSize);
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (!ImGui.Begin("##left-sidebar"u8, GuiTheme.SidebarFlags))
        {
            ImGui.End();
            return;
        }

        var mode = ctx.Mode;
        if (mode.LeftSidebar == LeftSidebarMode.Metrics)
        {
            comp?.DrawLeft(ref ctx);
            ImGui.End();
            return;
        }

        var state = stateContext.StateManager;

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));
        if (ImGui.BeginTabBar("##left-sidebar-tabs"u8, ImGuiTabBarFlags.FittingPolicyShrink))
        {
            var selected = mode.LeftSidebar;
            if (ImGui.BeginTabItem("Asset##asset-tab-btn"u8))
            {
                selected = LeftSidebarMode.Assets;
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Scene##scene-tab-btn"u8))
            {
                selected = LeftSidebarMode.Scene;
                ImGui.EndTabItem();
            }
            
            if(selected != mode.LeftSidebar)
                state.SetLeftSidebarState(selected);
            
            ImGui.EndTabBar();
        }


        if (comp is not null && ImGui.BeginChild("##left-sidebar"u8, ImGuiChildFlags.ResizeX))
        {
            comp.DrawLeft(ref ctx);
            ImGui.EndChild();
        }

        ImGui.PopStyleVar();
        ImGui.End();
    }


    private void DrawModeSelector()
    {
        var state = stateContext.StateManager;
        if (ImGui.BeginChild("##mode-select"u8))
        {
            var size = new Vector2(TopBtnWidth, GuiTheme.TopbarHeight);
            if (ImGui.Selectable("Metrics"u8, state.ModeState.IsMetricsMode, ImGuiSelectableFlags.None, size))
                state.SetViewModeState(ViewMode.Main, true);

            ImGui.SameLine();
            
            if (ImGui.Selectable("Editor"u8, state.ModeState.IsEditorMode, ImGuiSelectableFlags.None, size))
                state.SetViewModeState(ViewMode.Main, false);
        }

        ImGui.EndChild();
    }

    private void DrawPropertySelector(StateContext ctx)
    {
        var editorState = ctx.StateManager;
        var hasSelection = ctx.Selection.HasSelection();
        
        var count = hasSelection ? 3 : 2;
        var totalRightWidth = GuiTheme.TopbarBtnSize * count;
        var spacing = GuiTheme.ItemSpacing.X;
        totalRightWidth += spacing * count;
        var startPosX = ImGui.GetWindowWidth() - totalRightWidth - GuiTheme.WindowPadding.X;

        ImGui.SameLine(startPosX);

        if (ImGui.BeginChild("##prop-select"u8, new Vector2(0, GuiTheme.TopbarHeight)))
        {
            var state = editorState.ModeState.RightSidebar;
            var size = new Vector2(GuiTheme.TopbarBtnSize, GuiTheme.TopbarHeight);

            if (hasSelection && ImGui.Selectable("Property"u8, state == RightSidebarMode.SceneProperty, 0, size))
                editorState.ToggleRightSidebar(RightSidebarMode.SceneProperty);

            ImGui.SameLine();
            if (ImGui.Selectable("World"u8, state == RightSidebarMode.World, 0, size))
                editorState.ToggleRightSidebar(RightSidebarMode.World);

            ImGui.SameLine();
            if (ImGui.Selectable("Visual"u8, state == RightSidebarMode.Visuals, 0, size))
                editorState.ToggleRightSidebar(RightSidebarMode.Visuals);
        }

        ImGui.EndChild();
    }
}