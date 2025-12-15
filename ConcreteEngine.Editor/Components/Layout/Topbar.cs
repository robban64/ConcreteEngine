using System.Numerics;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

namespace ConcreteEngine.Editor.Components.Layout;

internal static class Topbar
{
    private static readonly string[] PropertyModes = ["Entity", "Camera", "World", "Sky", "Terrain"];

    public static void Draw()
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoResize;

        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(vp.WorkPos);
        ImGui.SetNextWindowSize(new Vector2(vp.Size.X, GuiTheme.TopbarHeight));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

        if (ImGui.Begin("##TopBar", flags))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));

            // left
            DrawModeSelector();

            // right
            if (StateContext.ModeState.IsEditorState)
                DrawPropertySelector();

            ImGui.PopStyleVar(1);
        }

        ImGui.End();
        ImGui.PopStyleVar(1);
    }

    private static void DrawModeSelector()
    {
        const int selectorWidth = 74;
        if (!ImGui.BeginChild("##editor-view-mode-selector")) return;

        if (ImGui.Selectable("Metrics", StateContext.ModeState.IsMetricState, ImGuiSelectableFlags.None,
                new Vector2(selectorWidth, GuiTheme.TopbarHeight)))
        {
            StateContext.SetViewModeState(EditorViewMode.Metrics);
        }

        ImGui.SameLine();
        if (ImGui.Selectable("Editor", StateContext.ModeState.IsEditorState, ImGuiSelectableFlags.None,
                new Vector2(selectorWidth, GuiTheme.TopbarHeight)))
        {
            StateContext.SetViewModeState(EditorViewMode.Editor);
        }

        ImGui.EndChild();
    }

    private static void DrawPropertySelector()
    {
        const float width = 64;
        var validEntity = EditorDataStore.SelectedEntity.IsValid;
        var count = validEntity ? PropertyModes.Length : PropertyModes.Length - 1;

        var totalRightWidth = width * count;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        totalRightWidth += spacing * (count - 1);
        var startPosX = ImGui.GetWindowWidth() - totalRightWidth - ImGui.GetStyle().WindowPadding.X;

        ImGui.SameLine(startPosX);

        if (!ImGui.BeginChild("##editor-property-selector", new Vector2(0, GuiTheme.TopbarHeight)))
            return;

        int idx = 0;
        for (var i = 0; i < PropertyModes.Length; i++)
        {
            if (i == 0 && !validEntity) continue;
            if (idx++ > 0) ImGui.SameLine();
            var selectorMode = PropertyIndexToEnum(i);
            var selected = selectorMode == StateContext.ModeState.RightSidebar;

            if (ImGui.Selectable(PropertyModes[i], selected, ImGuiSelectableFlags.None,
                    new Vector2(width, GuiTheme.TopbarHeight)))
            {
                StateContext.ToggleRightSidebar(selectorMode);
            }
        }

        ImGui.EndChild();
    }


    private static RightSidebarMode PropertyIndexToEnum(int index)
    {
        return index switch
        {
            0 => RightSidebarMode.Property,
            1 => RightSidebarMode.Camera,
            2 => RightSidebarMode.World,
            3 => RightSidebarMode.Sky,
            4 => RightSidebarMode.Terrain,
            _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
        };
    }
}