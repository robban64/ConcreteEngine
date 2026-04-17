using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class Topbar
{
    private static readonly Vector2 BtnSize = new(GuiTheme.TopbarHeight);

    private struct TopbarButton
    {
        private delegate*<StateContext, void> _onClick;
        public uint Icon;
        public bool Active, WasActive;
        private byte _flags;

        public TopbarButton(uint icon, delegate*<StateContext, void> onClick,
            ImGuiSelectableFlags flags = ImGuiSelectableFlags.None)
        {
            Icon = icon;
            _onClick = onClick;
            _flags = (byte)flags;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Draw(StateContext ctx, bool active)
        {
            var icon = Icon;

            if (Active && !active) WasActive = true;
            if (!Active && WasActive) WasActive = false;
            Active = active;

            var clicked = ImGui.Selectable((byte*)&icon, active, (ImGuiSelectableFlags)_flags, BtnSize);
            if (clicked) _onClick(ctx);
            return clicked;
        }
    }

    private readonly TopbarButton[] _buttons;
    private readonly StateContext _stateContext;

    static void ActivityClick(StateContext _stateContext)
    {
        _stateContext.EmitTransition(new TransitionMessage { Clear = true });
        _stateContext.EmitTransition(TransitionMessage.PushLeft(PanelId.MetricsLeft));
        _stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.MetricsRight));
    }

    static void MainModeClick(StateContext _stateContext)
    {
        _stateContext.EmitTransition(new TransitionMessage { Clear = true });
    }

    public Topbar(StateContext stateContext)
    {
        _stateContext = stateContext;
        _buttons = new TopbarButton[3];
        _buttons[0] = new TopbarButton(StyleMap.GetIntIcon(Icons.Activity), &ActivityClick);
        _buttons[1] = new TopbarButton(StyleMap.GetIntIcon(Icons.LayoutGrid), &MainModeClick);
        _buttons[2] = new TopbarButton(StyleMap.GetIntIcon(Icons.Play), &MainModeClick);
    }

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
        var length = _buttons.Length;

        var active = stackalloc bool[3] { _stateContext.IsMetricMode, !_stateContext.IsMetricMode, false };
        for (int i = 0; i < length; i++)
        {
            if (i > 0) ImGui.SameLine();
            _buttons[i].Draw(_stateContext, active[i]);
        }
    }

    private void DrawSelectedIcon()
    {
        var hasSelection = _stateContext.Selection.HasSelection();
        var propertyFlag = hasSelection ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled;
        if (ImGui.Selectable(StyleMap.GetIcon(Icons.MousePointer2), hasSelection, propertyFlag, BtnSize))
            _stateContext.EmitTransition(new TransitionMessage { Clear = true });
    }

    private void DrawInteractiveIcons(float width)
    {
        if (_stateContext.SelectedSceneObject is not { } inspectSceneObj) return;

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
            _stateContext.Selection.ToggleDrawBounds(!inspectSceneObj.ShowDebugBounds);
    }

    private void DrawSceneGraphicIcons()
    {
        var rightPanelId = _stateContext.Panels.RightPanelId;

        if (ImGui.Selectable(StyleMap.GetIcon(Icons.Video), rightPanelId == PanelId.Camera, 0, BtnSize))
            _stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.Camera));

        ImGui.SameLine();
        if (ImGui.Selectable(StyleMap.GetIcon(Icons.Sun), rightPanelId == PanelId.Lighting, 0, BtnSize))
            _stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.Lighting));

        ImGui.SameLine();
        if (ImGui.Selectable(StyleMap.GetIcon(Icons.CloudFog), rightPanelId == PanelId.Atmosphere, 0, BtnSize))
            _stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.Atmosphere));

        ImGui.SameLine();
        if (ImGui.Selectable(StyleMap.GetIcon(Icons.Sparkles), rightPanelId == PanelId.Visual, 0, BtnSize))
            _stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.Visual));
    }
}