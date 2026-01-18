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

    public PanelSize PanelSize;

    private EnumTabBar<LeftSidebarMode> _leftTabBar = new(2);
    private LeftSidebarMode _selected = LeftSidebarMode.Default;

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

        ImGui.SetNextWindowPos(PanelSize.RightPosition);
        ImGui.SetNextWindowSize(PanelSize.RightSize);
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
        ImGui.SetNextWindowPos(PanelSize.LeftPosition);
        ImGui.SetNextWindowSize(PanelSize.LeftSize);
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (!ImGui.Begin("##left-sidebar"u8, GuiTheme.SidebarFlags))
        {
            ImGui.End();
            return;
        }

        if (comp is not null && stateContext.IsActiveRight<MetricsComponent>())
        {
            comp?.DrawLeft(ref ctx);
            ImGui.End();
            return;
        }


        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));
        if (ImGui.BeginTabBar("##left-sidebar-tabs"u8, ImGuiTabBarFlags.FittingPolicyShrink))
        {
            var selected = LeftSidebarMode.Default;
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

            if (selected != _selected)
            {
                if (selected == LeftSidebarMode.Assets)
                    stateContext.EmitTransition(TransitionMessage.PushLeft(typeof(AssetsComponent)));
                if (selected == LeftSidebarMode.Scene)
                    stateContext.EmitTransition(TransitionMessage.PushLeft(typeof(SceneComponent)));

                _selected = selected;
            }

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
        if (ImGui.BeginChild("##mode-select"u8))
        {
            var isMetrics = stateContext.IsActiveRight<MetricsComponent>();
            var size = new Vector2(TopBtnWidth, GuiTheme.TopbarHeight);
            if (ImGui.Selectable("Metrics"u8, isMetrics, ImGuiSelectableFlags.None, size))
            {
                stateContext.EmitTransition(new TransitionMessage { Clear = true });
                stateContext.EmitTransition(TransitionMessage.PushLeft(typeof(MetricsComponent)));
                stateContext.EmitTransition(TransitionMessage.PushRight(typeof(MetricsComponent)));
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
            if (ImGui.Selectable("World"u8, ctx.IsActiveRight<WorldComponent>(), 0, size))
                ctx.EmitTransition(TransitionMessage.PushRight(typeof(WorldComponent)));

            ImGui.SameLine();
            if (ImGui.Selectable("Visual"u8, ctx.IsActiveRight<VisualComponent>(), 0, size))
                ctx.EmitTransition(TransitionMessage.PushRight(typeof(VisualComponent)));
        }

        ImGui.EndChild();
    }
}