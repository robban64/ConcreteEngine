using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Layout;

internal sealed class LeftSidebar
{
    public void Draw(ComponentRuntime? componentRuntime, StateManager states, FrameContext ctx, in PanelSize  panelSize)
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
            componentRuntime?.DrawLeft(in ctx);
            ImGui.End();
            return;
        }

        var isAssets = mode.LeftSidebar == LeftSidebarMode.Assets;
        var isScene = mode.LeftSidebar == LeftSidebarMode.Scene;

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));

        if (ImGui.BeginTabBar("##left-sidebar-tabs"u8, ImGuiTabBarFlags.FittingPolicyShrink))
        {
            if (isAssets) ImGui.PushStyleColor(ImGuiCol.Tab, GuiTheme.SelectedColor);
            if (ImGui.TabItemButton("Asset##asset-tab-btn"u8))
                states.SetLeftSidebarState(LeftSidebarMode.Assets);
            if (isAssets) ImGui.PopStyleColor();

            if (isScene) ImGui.PushStyleColor(ImGuiCol.Tab, GuiTheme.SelectedColor);
            if (ImGui.TabItemButton("Scene##scene-tab-btn"u8))
                states.SetLeftSidebarState(LeftSidebarMode.Scene);
            if (isScene) ImGui.PopStyleColor();

            ImGui.EndTabBar();
        }


        if (componentRuntime is not null && ImGui.BeginChild("##left-sidebar-scene"u8,ImGuiChildFlags.ResizeX))
        {
            componentRuntime.DrawLeft(in ctx);
            ImGui.EndChild();
        }

        ImGui.PopStyleVar();
        ImGui.End();
    }
}