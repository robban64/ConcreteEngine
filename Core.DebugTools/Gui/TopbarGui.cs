using System.Numerics;
using Core.DebugTools.Utils;
using ImGuiNET;

namespace Core.DebugTools.Gui;

internal static class TopbarGui
{
    private static readonly string[] Modes = ["None", "Editor", "Metrics"];
    
    private static int _currentMode = 0;

    public static void Draw()
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoResize;

        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(vp.WorkPos);
        ImGui.SetNextWindowSize(new Vector2(vp.Size.X, GuiTheme.TopbarHeight));
        //ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (ImGui.Begin("##TopBar", flags))
        {
            for (var i = 0; i < Modes.Length; i++)
            {
                if (i > 0) ImGui.SameLine();
                var selected = i == _currentMode;

                ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, GuiTheme.HoverColor);
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, GuiTheme.HoverColor);
                ImGui.PushStyleColor(ImGuiCol.Header, GuiTheme.PrimaryColor);

                if (ImGui.Selectable(Modes[i], selected, ImGuiSelectableFlags.None,
                        new Vector2(73, GuiTheme.TopbarHeight)))
                {
                    _currentMode = i;
                }

                ImGui.PopStyleColor(2);
                ImGui.PopStyleVar();
            }
        }

        ImGui.End();
        ImGui.PopStyleVar(1);
    }
}