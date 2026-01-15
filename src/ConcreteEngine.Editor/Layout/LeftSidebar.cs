using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Layout;

internal sealed class LeftSidebar
{
    public void Draw(ComponentRuntime? comp, StateManager states, FrameContext ctx, in PanelSize panelSize)
    {
        ImGui.SetNextWindowPos(panelSize.LeftPosition);
        ImGui.SetNextWindowSize(panelSize.LeftSize);
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (!ImGui.Begin("##left-sidebar"u8, GuiTheme.SidebarFlags))
        {
            ImGui.End();
            return;
        }

        var mode = ctx.Mode;
        if (mode.LeftSidebar == LeftSidebarMode.Metrics)
        {
            comp?.DrawLeft(in ctx);
            ImGui.End();
            return;
        }

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));

        if (ImGui.BeginTabBar("##left-sidebar-tabs"u8, ImGuiTabBarFlags.FittingPolicyShrink))
        {
            if (ImGui.BeginTabItem("Asset##asset-tab-btn"u8))
            {
                states.SetLeftSidebarState(LeftSidebarMode.Assets);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Scene##scene-tab-btn"u8))
            {
                states.SetLeftSidebarState(LeftSidebarMode.Scene);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }


        if (comp is not null && ImGui.BeginChild("##left-sidebar"u8, ImGuiChildFlags.ResizeX))
        {
            comp.DrawLeft(in ctx);
            ImGui.EndChild();
        }

        ImGui.PopStyleVar();
        ImGui.End();
    }
}