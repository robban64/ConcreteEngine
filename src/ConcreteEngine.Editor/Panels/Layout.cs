using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class Layout(StateContext stateContext)
{
    private const int TopBtnWidth = 74;

    private enum LeftSidebarTabs : byte { Asset, Scene }

    public PanelSize PanelSize;

    private readonly EnumTabBar<LeftSidebarTabs> _leftTabBar = new(-1, ImGuiTabBarFlags.FittingPolicyShrink);

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

    public void DrawRight(EditorPanel? panel, FrameContext ctx)
    {
        if (panel == null) return;

        ImGui.SetNextWindowPos(PanelSize.RightPosition);
        ImGui.SetNextWindowSize(PanelSize.RightSize);
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (!ImGui.Begin("##right-sidebar"u8, GuiTheme.SidebarFlags))
        {
            ImGui.End();
            return;
        }

        ImGui.PushID("##right-sidebar-body"u8);
        panel.Draw(ref ctx);
        ImGui.PopID();

        ImGui.End();
    }

    public void DrawLeft(EditorPanel? panel, FrameContext ctx)
    {
        ImGui.SetNextWindowPos(PanelSize.LeftPosition);
        ImGui.SetNextWindowSize(PanelSize.LeftSize);
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (!ImGui.Begin("##left-sidebar"u8, GuiTheme.SidebarFlags))
        {
            ImGui.End();
            return;
        }

        if (panel is not null && stateContext.State.RightPanelId == PanelId.MetricsRight)
        {
            panel.Draw(ref ctx);
            ImGui.End();
            return;
        }

        if (_leftTabBar.Draw(out var selected))
        {
            if (selected == LeftSidebarTabs.Asset)
                stateContext.EmitTransition(TransitionMessage.PushLeft(PanelId.AssetList));
            if (selected == LeftSidebarTabs.Scene)
                stateContext.EmitTransition(TransitionMessage.PushLeft(PanelId.SceneList));
        }

        if (panel is not null && ImGui.BeginChild("##left-sidebar"u8, ImGuiChildFlags.ResizeX))
        {
            panel.Draw(ref ctx);
            ImGui.EndChild();
        }

        ImGui.End();
    }


    private void DrawModeSelector()
    {
        if (ImGui.BeginChild("##mode-select"u8))
        {
            var isMetrics = stateContext.State.RightPanelId == PanelId.MetricsRight;
            var size = new Vector2(TopBtnWidth, GuiTheme.TopbarHeight);
            if (ImGui.Selectable("Metrics"u8, isMetrics, ImGuiSelectableFlags.None, size))
            {
                stateContext.EmitTransition(new TransitionMessage { Clear = true });
                stateContext.EmitTransition(TransitionMessage.PushLeft(PanelId.MetricsLeft));
                stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.MetricsRight));
            }

            ImGui.SameLine();

            if (ImGui.Selectable("Editor"u8, !isMetrics, ImGuiSelectableFlags.None, size))
                stateContext.EmitTransition(new TransitionMessage { Clear = true });
        }

        ImGui.EndChild();
    }

    private void DrawPropertySelector(StateContext ctx)
    {
        var hasSelection = ctx.Selection.HasSelection();
        var state = ctx.State;

        var count = hasSelection ? 3 : 2;
        var totalRightWidth = GuiTheme.TopbarBtnSize * count;
        var spacing = GuiTheme.ItemSpacing.X;
        totalRightWidth += spacing * count;
        var startPosX = ImGui.GetWindowWidth() - totalRightWidth - GuiTheme.WindowPadding.X;

        ImGui.SameLine(startPosX);

        if (ImGui.BeginChild("##prop-select"u8, new Vector2(0, GuiTheme.TopbarHeight)))
        {
            var size = new Vector2(GuiTheme.TopbarBtnSize, GuiTheme.TopbarHeight);
            if (hasSelection)
            {
                var active = ctx.Selection.HasSelection();
                if (ImGui.Selectable("Property"u8, active, 0, size))
                    ctx.EmitTransition(new TransitionMessage { Clear = true });
            }


            ImGui.SameLine();
            if (ImGui.Selectable("World"u8, state.RightPanelId == PanelId.World, 0, size))
                ctx.EmitTransition(TransitionMessage.PushRight(PanelId.World));

            ImGui.SameLine();
            if (ImGui.Selectable("Visual"u8, state.RightPanelId == PanelId.Visual, 0, size))
                ctx.EmitTransition(TransitionMessage.PushRight(PanelId.Visual));
        }

        ImGui.EndChild();
    }
}