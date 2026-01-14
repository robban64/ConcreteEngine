using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Layout;

internal sealed class RightSidebar
{
    public void Draw(ComponentRuntime componentRuntime, FrameContext ctx, in PanelSize panelSize)
    {
        ImGui.SetNextWindowPos(panelSize.RightPosition);
        ImGui.SetNextWindowSize(panelSize.RightSize);
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (!ImGui.Begin("##right-sidebar"u8, GuiTheme.SidebarFlags))
        {
            ImGui.End();
            return;
        }

        ImGui.PushID("##right-sidebar-body"u8);
        componentRuntime.DrawRight(in ctx);
        ImGui.PopID();

        ImGui.End();
    }
}