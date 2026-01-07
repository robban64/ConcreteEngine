using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components.Layout;

internal static class Topbar
{
    //private static readonly string[] PropertyModes = ["Entity", "Camera", "World", "Sky", "Terrain"];

    public static void Draw()
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse;

        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(vp.WorkPos);
        ImGui.SetNextWindowSize(vp.Size with { Y = GuiTheme.TopbarHeight });
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

        if (ImGui.Begin("##TopBar"u8, flags))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));

            // left
            DrawModeSelector();

            // right
            if (StateContext.ModeState.IsEditorState)
                DrawPropertySelector();

            ImGui.PopStyleVar(1);
            ImGui.End();
        }

        ImGui.PopStyleVar(1);
    }

    private static void DrawModeSelector()
    {
        const int selectorWidth = 74;
        var size = new Vector2(selectorWidth, GuiTheme.TopbarHeight);
        if (ImGui.BeginChild("##editor-view-mode-selector"u8))
        {
            if (ImGui.Selectable("Metrics"u8, StateContext.ModeState.IsMetricState, ImGuiSelectableFlags.None, size))
            {
                StateContext.SetViewModeState(ViewMode.Metrics);
            }

            ImGui.SameLine();
            if (ImGui.Selectable("Editor"u8, StateContext.ModeState.IsEditorState, ImGuiSelectableFlags.None, size))
            {
                StateContext.SetViewModeState(ViewMode.Editor);
            }
        }

        ImGui.EndChild();
    }

    private static void DrawPropertySelector()
    {
        const float width = 64;
        var validEntity =  EditorDataStore.SelectedSceneObj.IsValid();
        var count = validEntity ? 5 : 4;

        var totalRightWidth = width * count;
        var spacing = GuiTheme.ItemSpacing.X;
        totalRightWidth += spacing * count;
        var startPosX = ImGui.GetWindowWidth() - totalRightWidth - GuiTheme.WindowPadding.X;

        ImGui.SameLine(startPosX);

        if (ImGui.BeginChild("##editor-property-selector"u8, new Vector2(0, GuiTheme.TopbarHeight)))
        {
            DrawItems(validEntity);
        }

        ImGui.EndChild();

        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void DrawItems(bool validEntity)
        {
            var state = StateContext.ModeState.RightSidebar;
            var size = new Vector2(width, GuiTheme.TopbarHeight);

            if (validEntity && ImGui.Selectable("Entity"u8, state == RightSidebarMode.Property, 0, size))
                StateContext.ToggleRightSidebar(RightSidebarMode.Property);

            ImGui.SameLine();
            if (ImGui.Selectable("Camera"u8, state == RightSidebarMode.Camera, 0, size))
                StateContext.ToggleRightSidebar(RightSidebarMode.Camera);

            ImGui.SameLine();
            if (ImGui.Selectable("World"u8, state == RightSidebarMode.World, 0, size))
                StateContext.ToggleRightSidebar(RightSidebarMode.World);

            ImGui.SameLine();
            if (ImGui.Selectable("Sky"u8, state == RightSidebarMode.Sky, 0, size))
                StateContext.ToggleRightSidebar(RightSidebarMode.Sky);

            ImGui.SameLine();
            if (ImGui.Selectable("Terrain"u8, state == RightSidebarMode.Terrain, 0, size))
                StateContext.ToggleRightSidebar(RightSidebarMode.Terrain);
        }
    }
}