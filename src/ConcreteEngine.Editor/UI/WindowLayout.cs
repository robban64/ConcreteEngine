using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class WindowLayout(StateContext stateContext)
{
    private const ImGuiWindowFlags ConsoleWindowFlags =
        ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus |
        ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoTitleBar |
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;


    private static PanelSize _panelSize;
    private static ConsoleWindowSize _consoleSize;
    private static readonly Vector2 BtnSize = new(GuiTheme.TopbarHeight);
    private static readonly Vector2 ConsolePadding = new(12f, 6f);
    private static readonly Vector2 SidebarTabFramePadding = new(12f, 4f);

    private readonly PanelState _panels = stateContext.Panels;

    public void DrawPanels(FrameContext ctx)
    {
        var panels = _panels;
        if (ImGui.Begin("left-sidebar"u8))
        {
            DrawLeftSidebarHeader();
            ImGui.PushID((int)panels.LeftPanelId);
            panels.Left.Draw(ctx);
            ImGui.PopID();
        }

        ImGui.End();

        if (ImGui.Begin("right-sidebar"u8))
        {
            bool childVisible = ImGui.BeginChild("body"u8, ImGuiChildFlags.AlwaysUseWindowPadding);
            if (childVisible)
            {
                ImGui.PushID((int)panels.RightPanelId);
                panels.Right.Draw(ctx);
                ImGui.PopID();
            }

            ImGui.EndChild();
        }

        ImGui.End();
    }

    public void DrawLayout()
    {
        // top
        {
            var vp = ImGui.GetMainViewport();
            float vpWidth = vp.Size.X;

            ImGui.SetNextWindowPos(vp.WorkPos);
            ImGui.SetNextWindowSize(new Vector2(vpWidth, GuiTheme.TopbarHeight));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

            if (ImGui.Begin("topbar"u8, GuiTheme.TopbarFlags))
                DrawTopbar(vpWidth);

            ImGui.End();
            ImGui.PopStyleVar();
        }

        // sidebar
        {
            // left
            ref readonly var panelSize = ref _panelSize;
            ImGui.SetNextWindowPos(panelSize.LeftPosition);
            ImGui.SetNextWindowSize(panelSize.LeftSize);
            ImGui.Begin("left-sidebar"u8, GuiTheme.SidebarFlags);


            ImGui.End();

            // right
            ImGui.SetNextWindowPos(panelSize.RightPosition);
            ImGui.SetNextWindowSize(panelSize.RightSize);

            ImGui.Begin("right-sidebar"u8, GuiTheme.SidebarFlags);
            ImGui.End();
        }

        // console
        {
            ref readonly var layout = ref _consoleSize;
            ImGui.SetNextWindowPos(layout.Position);
            ImGui.SetNextWindowSize(layout.Size);
            ImGui.SetNextWindowSizeConstraints(layout.SizeConstraintMin, layout.SizeConstraintMax);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, GuiTheme.ConsoleBgColor);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, ConsolePadding);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);

            ImGui.Begin("cli"u8, ConsoleWindowFlags);
            ImGui.End();

            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
        }
    }

    private void DrawLeftSidebarHeader()
    {
        if (stateContext.IsMetricMode) return;

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, SidebarTabFramePadding);

        if (ImGui.BeginTabBar("##panel-tabs"u8, ImGuiTabBarFlags.FittingPolicyShrink))
        {
            var leftPanelId = stateContext.Panels.LeftPanelId;

            if (ImGui.BeginTabItem("Asset"u8))
            {
                if (leftPanelId != PanelId.AssetList)
                    stateContext.EmitTransition(TransitionMessage.PushLeft(PanelId.AssetList));
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Scene"u8))
            {
                if (leftPanelId != PanelId.SceneList)
                    stateContext.EmitTransition(TransitionMessage.PushLeft(PanelId.SceneList));
                ImGui.EndTabItem();
            }
            
            ImGui.EndTabBar();
        }

        ImGui.PopStyleVar();
    }


    private void DrawTopbar(float width)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));
        ImGui.PushStyleColor(ImGuiCol.Text, Color4.White);
        ImGui.PushStyleColor(ImGuiCol.Header, Palette.PrimaryColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Palette.HoverColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, Palette.SelectedColor);

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


    public void CalculatePanelSize()
    {
        var vp = ImGui.GetMainViewport();
        var height = vp.WorkSize.Y - GuiTheme.TopbarHeight;
        var hasLeftSidebar = stateContext.Panels.LeftPanelId != PanelId.None;
        var leftHeight = hasLeftSidebar ? height : 52;

        var isEditor = stateContext.Panels.RightPanelId != PanelId.MetricsRight;
        var left = isEditor ? GuiTheme.LeftSidebarDefaultWidth : GuiTheme.LeftSidebarCompactWidth;
        var right = isEditor ? GuiTheme.RightSidebarDefaultWidth : GuiTheme.RightSidebarCompactWidth;

        ref var panelSize = ref _panelSize;
        panelSize.LeftSize = new Vector2(left, leftHeight);
        panelSize.LeftPosition = vp.WorkPos with { Y = vp.WorkPos.Y + GuiTheme.TopbarHeight };
        panelSize.RightSize = new Vector2(right, height);
        panelSize.RightPosition =
            new Vector2(vp.WorkPos.X + vp.WorkSize.X - right, vp.WorkPos.Y + GuiTheme.TopbarHeight);

        CalculateConsoleSize(in vp, left, right);
    }

    private static void CalculateConsoleSize(in ImGuiViewportPtr vp, float leftPanelWidth, float rightPanelWidth)
    {
        const float minW = 400f, maxWCap = 980f;
        const float minH = 240f, maxH = 300f;
        const float margin = 12f;

        var centerX = vp.WorkPos.X + leftPanelWidth;
        var centerY = vp.WorkPos.Y;
        var centerW = MathF.Max(0, vp.WorkSize.X - leftPanelWidth - rightPanelWidth);
        var centerH = vp.WorkSize.Y;

        var targetW = float.Clamp(centerW * 0.80f, minW, Math.Min(maxWCap, centerW));
        var targetH = float.Clamp(centerH * 0.25f, minH, maxH);

        var posX = centerX + MathF.Max(0, (centerW - targetW) * 0.5f);
        var posY = centerY + centerH - targetH - margin;

        ref var consoleSize = ref _consoleSize;
        consoleSize.Position = new Vector2(posX, posY);
        consoleSize.Size = new Vector2(targetW, targetH);
        consoleSize.SizeConstraintMin = new Vector2(MathF.Min(minW, centerW), minH);
        consoleSize.SizeConstraintMax =
            new Vector2(MathF.Min(float.Min(maxWCap, centerW), centerW), MathF.Min(maxH, centerH));
    }
}