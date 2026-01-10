using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components.Layout;

internal static class RightSidebar
{
    public static int Width;

    public static void Draw(ModelStateComponent ctx, StateManager states)
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                                       ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        var vp = ImGui.GetMainViewport();
        var vpSize = vp.WorkSize;

        var height = !states.ModeState.IsActive ? 0 : vpSize.Y - GuiTheme.TopbarHeight;

        ImGui.SetNextWindowPos(new Vector2(vpSize.X - Width, GuiTheme.TopbarHeight));
        ImGui.SetNextWindowSize(new Vector2(Width, height));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (!ImGui.Begin("##RightSidebar"u8, flags))
        {
            ImGui.End();
            return;
        }
        
        ctx.DrawRight();
        ImGui.End();
    }

}