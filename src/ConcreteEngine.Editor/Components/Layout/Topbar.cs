using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Layout;

internal sealed class Topbar
{
    public void Draw(StateContext ctx)
    {
        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(vp.WorkPos);
        ImGui.SetNextWindowSize(vp.Size with { Y = GuiTheme.TopbarHeight });
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

        if (ImGui.Begin("##topbar"u8, GuiTheme.TopbarFlags))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));

            // left
            DrawModeSelector(ctx);

            // right
            DrawPropertySelector(ctx);

            ImGui.PopStyleVar(1);
            ImGui.End();
        }

        ImGui.PopStyleVar(1);
    }

    private void DrawModeSelector(StateContext ctx)
    {
        const int selectorWidth = 74;
        var size = new Vector2(selectorWidth, GuiTheme.TopbarHeight);
        var state = ctx.EditorState;
        if (ImGui.BeginChild("##editor-view-mode-selector"u8))
        {
            if (ImGui.Selectable("Metrics"u8, state.ModeState.IsMetricsMode, ImGuiSelectableFlags.None, size))
            {
                state.SetViewModeState(ViewMode.Main, true);
            }

            ImGui.SameLine();
            if (ImGui.Selectable("Editor"u8, state.ModeState.IsEditorMode, ImGuiSelectableFlags.None, size))
            {
                state.SetViewModeState(ViewMode.Main, false);
            }
        }

        ImGui.EndChild();
    }

    private void DrawPropertySelector(StateContext ctx)
    {
        const float width = 64;
        var validEntity = ctx.Selection.SelectedSceneId.IsValid();
        var editorState = ctx.EditorState;
        var count = validEntity ? 5 : 4;

        var totalRightWidth = width * count;
        var spacing = GuiTheme.ItemSpacing.X;
        totalRightWidth += spacing * count;
        var startPosX = ImGui.GetWindowWidth() - totalRightWidth - GuiTheme.WindowPadding.X;

        ImGui.SameLine(startPosX);

        if (ImGui.BeginChild("##editor-property-selector"u8, new Vector2(0, GuiTheme.TopbarHeight)))
        {
            var state = editorState.ModeState.RightSidebar;
            var size = new Vector2(width, GuiTheme.TopbarHeight);

            if (validEntity && ImGui.Selectable("Property"u8, state == RightSidebarMode.SceneProperty, 0, size))
                editorState.ToggleRightSidebar(RightSidebarMode.SceneProperty);

            ImGui.SameLine();
            if (ImGui.Selectable("Camera"u8, state == RightSidebarMode.Camera, 0, size))
                editorState.ToggleRightSidebar(RightSidebarMode.Camera);

            ImGui.SameLine();
            if (ImGui.Selectable("World"u8, state == RightSidebarMode.World, 0, size))
                editorState.ToggleRightSidebar(RightSidebarMode.World);

            ImGui.SameLine();
            if (ImGui.Selectable("Sky"u8, state == RightSidebarMode.Sky, 0, size))
                editorState.ToggleRightSidebar(RightSidebarMode.Sky);

            ImGui.SameLine();
            if (ImGui.Selectable("Terrain"u8, state == RightSidebarMode.Terrain, 0, size))
                editorState.ToggleRightSidebar(RightSidebarMode.Terrain);
        }

        ImGui.EndChild();
    }
}