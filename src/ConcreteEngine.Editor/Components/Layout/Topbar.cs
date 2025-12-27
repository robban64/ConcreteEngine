using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

namespace ConcreteEngine.Editor.Components.Layout;

internal static class Topbar
{
    //private static readonly string[] PropertyModes = ["Entity", "Camera", "World", "Sky", "Terrain"];

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
        var validEntity = EditorDataStore.SelectedEntity.IsValid || EditorDataStore.SelectedSceneObject.IsValid;
        var count = validEntity ? 5 : 4;

        var totalRightWidth = width * count;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        totalRightWidth += spacing * (count - 1);
        var startPosX = ImGui.GetWindowWidth() - totalRightWidth - ImGui.GetStyle().WindowPadding.X;

        ImGui.SameLine(startPosX);

        if (!ImGui.BeginChild("##editor-property-selector", new Vector2(0, GuiTheme.TopbarHeight)))
            return;
        
        DrawItems(validEntity);

        ImGui.EndChild();
        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void DrawItems(bool validEntity)
        {
            var state = StateContext.ModeState.RightSidebar;
            var size = new Vector2(width, GuiTheme.TopbarHeight);

            if (validEntity && ImGui.Selectable("Entity", state == RightSidebarMode.Property, 0, size))
                StateContext.ToggleRightSidebar(RightSidebarMode.Property);

            ImGui.SameLine();
            if (ImGui.Selectable("Camera", state == RightSidebarMode.Camera, 0, size))
                StateContext.ToggleRightSidebar(RightSidebarMode.Camera);

            ImGui.SameLine();
            if (ImGui.Selectable("World", state == RightSidebarMode.World, 0, size))
                StateContext.ToggleRightSidebar(RightSidebarMode.World);

            ImGui.SameLine();
            if (ImGui.Selectable("Sky", state == RightSidebarMode.Sky, 0, size))
                StateContext.ToggleRightSidebar(RightSidebarMode.Sky);

            ImGui.SameLine();
            if (ImGui.Selectable("Terrain", state == RightSidebarMode.Terrain, 0, size))
                StateContext.ToggleRightSidebar(RightSidebarMode.Terrain);
        }
    }

}