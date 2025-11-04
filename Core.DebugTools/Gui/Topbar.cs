using System.Numerics;
using Core.DebugTools.Data;
using Core.DebugTools.Definitions;
using Core.DebugTools.Utils;
using ImGuiNET;

namespace Core.DebugTools.Gui;

internal sealed class Topbar
{
    private static readonly string[] Modes = Enum.GetNames<EditorViewMode>();

    private readonly EditorStateContext _ctx;

    public Topbar(EditorStateContext ctx)
    {
        _ctx = ctx;
    }

    private void OnViewModeChanged(EditorViewMode mode) => _ctx.SetViewMode(mode);
    
    public void Draw()
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
            ImGui.SetWindowFontScale(1.06f);
            for (var i = 0; i < Modes.Length; i++)
            {
                if (i > 0) ImGui.SameLine();
                var selected = i == (int)_ctx.ViewMode;

                ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, GuiTheme.HoverColor);
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, GuiTheme.HoverColor);
                ImGui.PushStyleColor(ImGuiCol.Header, GuiTheme.PrimaryColor);

                if (ImGui.Selectable(Modes[i], selected, ImGuiSelectableFlags.None,
                        new Vector2(73, GuiTheme.TopbarHeight)))
                {
                    OnViewModeChanged((EditorViewMode)i);
                }

                ImGui.PopStyleColor(2);
                ImGui.PopStyleVar();
            }
        }

        ImGui.End();
        ImGui.PopStyleVar(1);
    }
}