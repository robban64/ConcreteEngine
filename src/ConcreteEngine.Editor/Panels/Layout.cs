using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Data;
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

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        if (ImGui.Begin("##topbar"u8, GuiTheme.TopbarFlags))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));
            ImGui.PushStyleColor(ImGuiCol.Text, Color4.White);

            DrawModeSelector(vp.Size.X);

            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            ImGui.End();
        }

        ImGui.PopStyleVar();
    }


    public void DrawLeft(EditorPanel? panel, in FrameContext ctx)
    {
        ImGui.SetNextWindowPos(PanelSize.LeftPosition);
        ImGui.SetNextWindowSize(PanelSize.LeftSize);

        if (!ImGui.Begin("##left-sidebar"u8, GuiTheme.SidebarFlags))
        {
            ImGui.End();
            return;
        }

        if (!stateContext.IsMetricMode() && _leftTabBar.Draw(ctx.Writer, out var selected))
        {
            if (selected == LeftSidebarTabs.Asset)
                stateContext.EmitTransition(TransitionMessage.PushLeft(PanelId.AssetList));
            if (selected == LeftSidebarTabs.Scene)
                stateContext.EmitTransition(TransitionMessage.PushLeft(PanelId.SceneList));
        }

        panel?.Draw(in ctx);

        ImGui.End();
    }

    public void DrawRight(EditorPanel? panel, in FrameContext ctx)
    {
        if (panel == null) return;

        ImGui.SetNextWindowPos(PanelSize.RightPosition);
        ImGui.SetNextWindowSize(PanelSize.RightSize);

        if (!ImGui.Begin("##right-sidebar"u8, GuiTheme.SidebarFlags))
        {
            ImGui.End();
            return;
        }

        panel.Draw(in ctx);

        ImGui.End();
    }

    private void DrawModeSelector(float width)
    {
        var ctx = stateContext;

        var state = ctx.State;
        var hasSelection = ctx.Selection.HasSelection();
        var isMetrics = ctx.IsMetricMode();

        var size = new Vector2(TopBtnWidth, GuiTheme.TopbarHeight);

        if (ImGui.Selectable("Metrics"u8, isMetrics, 0, size))
        {
            ctx.EmitTransition(new TransitionMessage { Clear = true });
            ctx.EmitTransition(TransitionMessage.PushLeft(PanelId.MetricsLeft));
            ctx.EmitTransition(TransitionMessage.PushRight(PanelId.MetricsRight));
        }

        ImGui.SameLine();

        if (ImGui.Selectable("Editor"u8, !isMetrics, 0, size))
            ctx.EmitTransition(new TransitionMessage { Clear = true });

        //
        ImGui.SameLine(width - (size.X * 3) - GuiTheme.WindowPadding.X * 2);
        //

        var propertyFlag = hasSelection ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled;
        if (ImGui.Selectable("Property"u8, hasSelection, propertyFlag, size))
            ctx.EmitTransition(new TransitionMessage { Clear = true });

        ImGui.SameLine();
        if (ImGui.Selectable("World"u8, state.RightPanelId == PanelId.World, 0, size))
            ctx.EmitTransition(TransitionMessage.PushRight(PanelId.World));

        ImGui.SameLine();
        if (ImGui.Selectable("Visual"u8, state.RightPanelId == PanelId.Visual, 0, size))
            ctx.EmitTransition(TransitionMessage.PushRight(PanelId.Visual));
    }
}