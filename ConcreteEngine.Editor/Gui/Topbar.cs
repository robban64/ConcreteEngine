#region

using System.Numerics;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor.Gui;

internal static class Topbar
{
    private const int SelectorWidth = 74;
    private static readonly string[] ViewModes = ["Editor", "Metrics"];
    private static readonly string[] PropertyModes = ["Camera", "Light", "Sky", "Terrain"];


    public static void Draw()
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoResize;

        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(vp.WorkPos);
        ImGui.SetNextWindowSize(new Vector2(vp.Size.X, GuiTheme.TopbarHeight));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (ImGui.Begin("##TopBar", flags))
        {
            ImGui.SetWindowFontScale(1.06f);
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, GuiTheme.SelectedColor);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, GuiTheme.SelectedColor);
            ImGui.PushStyleColor(ImGuiCol.Header, GuiTheme.PrimaryColor);

            // left
            DrawModeSelector();

            // right
            if (StateCtx.ViewState.IsEditorState)
                DrawPropertySelector();

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar(1);
        }

        ImGui.End();
        ImGui.PopStyleVar(1);
    }

    private static void DrawModeSelector()
    {
        if (!ImGui.BeginChild("##editor-view-mode-selector")) return;
        for (var i = 0; i < ViewModes.Length; i++)
        {
            if (i > 0) ImGui.SameLine();
            var viewMode = ViewModeIndexToEnum(i);
            var selected = viewMode == StateCtx.ViewState.EditorMode;

            if (ImGui.Selectable(ViewModes[i], selected, ImGuiSelectableFlags.None,
                    new Vector2(SelectorWidth, GuiTheme.TopbarHeight)))
            {
                StateCtx.SetViewModeState(viewMode);
            }
        }

        ImGui.EndChild();
    }

    private static void DrawPropertySelector()
    {
        float x = ImGui.GetContentRegionAvail().X;
        float startPosX = x - 74 / 2f - GuiTheme.TopbarHeight * 5;
        ImGui.SameLine(startPosX);

        if (!ImGui.BeginChild("##editor-property-selector")) return;
        for (var i = 0; i < PropertyModes.Length; i++)
        {
            if (i > 0) ImGui.SameLine();
            var selectorMode = PropertyIndexToEnum(i);
            var selected = selectorMode == StateCtx.ViewState.RightSidebar;

            if (ImGui.Selectable(PropertyModes[i], selected, ImGuiSelectableFlags.None,
                    new Vector2(GuiTheme.TopbarHeight, GuiTheme.TopbarHeight)))
            {
                StateCtx.ToggleRightSidebarState(selectorMode);
            }
        }

        ImGui.EndChild();
    }

    private static EditorViewMode ViewModeIndexToEnum(int index)
    {
        return index switch
        {
            0 => EditorViewMode.Editor,
            1 => EditorViewMode.Metrics,
            _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
        };
    }

    private static RightSidebarMode PropertyIndexToEnum(int index)
    {
        return index switch
        {
            0 => RightSidebarMode.Camera,
            1 => RightSidebarMode.Light,
            2 => RightSidebarMode.Sky,
            3 => RightSidebarMode.Terrain,
            _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
        };
    }
}