using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class Topbar(StateContext stateContext)
{
    private static readonly Vector2 BtnSize = new(GuiTheme.TopbarHeight);

    public void Draw(float width)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));
        ImGui.PushStyleColor(ImGuiCol.Text, Palette32.White);
        ImGui.PushStyleColor(ImGuiCol.Header, Palette32.PrimaryColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Palette32.HoverColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, Palette32.SelectedColor);

        GuiTheme.PushFontIconLarge();

        DrawModeIcons();
        //
        DrawInteractiveIcons(width);
        //
        ImGui.SameLine(width - (BtnSize.X * 5) - GuiTheme.WindowPadding.X * 2 - 12.0f);
        DrawSelectedIcon();
        ImGui.SameLine();
        DrawSceneGraphicIcons();

        ImGui.PopFont();
        ImGui.PopStyleColor(4);
        ImGui.PopStyleVar();
    }

    private void DrawModeIcons()
    {
        if (ImGui.Selectable(StyleMap.GetIcon(Icons.Activity), stateContext.IsMetricMode, 0, BtnSize))
        {
            stateContext.EmitTransition(new TransitionMessage { Clear = true });
            stateContext.EmitTransition(TransitionMessage.PushLeft(PanelId.MetricsLeft));
            stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.MetricsRight));
        }

        ImGui.SameLine();
        if (ImGui.Selectable(StyleMap.GetIcon(Icons.LayoutGrid), !stateContext.IsMetricMode, 0, BtnSize))
            stateContext.EmitTransition(new TransitionMessage { Clear = true });

        ImGui.SameLine();
        if (ImGui.Selectable(StyleMap.GetIcon(Icons.Play), false, 0, BtnSize)) ;
    }

    private void DrawSelectedIcon()
    {
        var hasSelection = stateContext.Selection.HasSelection();
        var propertyFlag = hasSelection ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled;
        if (ImGui.Selectable(StyleMap.GetIcon(Icons.MousePointer2), hasSelection, propertyFlag, BtnSize))
            stateContext.EmitTransition(new TransitionMessage { Clear = true });
    }

    private void DrawInteractiveIcons(float width)
    {
        if (stateContext.SelectedSceneObject is not { } inspectSceneObj) return;

        var op = EditorInputState.GizmoOperation;

        ImGui.SameLine(width * 0.5f - (BtnSize.X * 3f / 2f));

        if (ImGui.Selectable(StyleMap.GetIcon(Icons.Move3d), op == ImGuizmoOperation.Translate, 0, BtnSize))
            EditorInputState.GizmoOperation = ImGuizmoOperation.Translate;

        ImGui.SameLine();
        if (ImGui.Selectable(StyleMap.GetIcon(Icons.Scale3d), op == ImGuizmoOperation.Scale, 0, BtnSize))
            EditorInputState.GizmoOperation = ImGuizmoOperation.Scale;

        ImGui.SameLine();
        if (ImGui.Selectable(StyleMap.GetIcon(Icons.Rotate3d), op == ImGuizmoOperation.Rotate, 0, BtnSize))
            EditorInputState.GizmoOperation = ImGuizmoOperation.Rotate;

        ImGui.SameLine();
        if (ImGui.Selectable(StyleMap.GetIcon(Icons.Box), inspectSceneObj.ShowDebugBounds, 0, BtnSize))
            stateContext.Selection.ToggleDrawBounds(!inspectSceneObj.ShowDebugBounds);
    }

    private void DrawSceneGraphicIcons()
    {
        var rightPanelId = stateContext.Panels.RightPanelId;

        if (ImGui.Selectable(StyleMap.GetIcon(Icons.Video), rightPanelId == PanelId.Camera, 0, BtnSize))
            stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.Camera));

        ImGui.SameLine();
        if (ImGui.Selectable(StyleMap.GetIcon(Icons.Sun), rightPanelId == PanelId.Lighting, 0, BtnSize))
            stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.Lighting));

        ImGui.SameLine();
        if (ImGui.Selectable(StyleMap.GetIcon(Icons.CloudFog), rightPanelId == PanelId.Atmosphere, 0, BtnSize))
            stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.Atmosphere));

        ImGui.SameLine();
        if (ImGui.Selectable(StyleMap.GetIcon(Icons.Sparkles), rightPanelId == PanelId.Visual, 0, BtnSize))
            stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.Visual));
    }
}