using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class WindowLayout(StateContext stateContext)
{
    private const ImGuiWindowFlags ConsoleWindowFlags =
        ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus |
        ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoTitleBar |
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

    private PanelSize _panelSize;
    private ConsoleWindowSize _consoleSize;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawPanels(in FrameContext ctx)
    {
        var panels = stateContext.State;

        ImGui.Begin("left-sidebar"u8);
        panels.Left.Draw(in ctx);
        ImGui.End();

        ImGui.Begin("right-sidebar"u8);
        ImGui.BeginChild("body"u8, ImGuiChildFlags.AlwaysUseWindowPadding);

        ImGui.PushID((int)panels.Right.Id);
        panels.Right.Draw(in ctx);
        ImGui.PopID();

        ImGui.EndChild();
        ImGui.End();
    }

    public void DrawLayout(in FrameContext ctx)
    {
        var vp = ImGui.GetMainViewport();

        // top
        {
            ImGui.SetNextWindowPos(vp.WorkPos);
            ImGui.SetNextWindowSize(vp.Size with { Y = GuiTheme.TopbarHeight });
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

            ImGui.Begin("topbar"u8, GuiTheme.TopbarFlags);
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));
            ImGui.PushStyleColor(ImGuiCol.Text, Color4.White);

            DrawTopbar(vp.Size.X, ctx);

            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            ImGui.End();
            ImGui.PopStyleVar();
        }

        // sidebar
        {
            // left
            scoped ref readonly var panelSize = ref _panelSize;
            ImGui.SetNextWindowPos(panelSize.LeftPosition);
            ImGui.SetNextWindowSize(panelSize.LeftSize);
            ImGui.Begin("left-sidebar"u8, GuiTheme.SidebarFlags);

            if (!stateContext.IsMetricMode())
                DrawLeftSidebarHeader();

            ImGui.End();

            // right
            ImGui.SetNextWindowPos(panelSize.RightPosition);
            ImGui.SetNextWindowSize(panelSize.RightSize);

            ImGui.Begin("right-sidebar"u8, GuiTheme.SidebarFlags);
            ImGui.End();
        }

        // console
        {
            scoped ref readonly var layout = ref _consoleSize;
            ImGui.SetNextWindowPos(layout.Position);
            ImGui.SetNextWindowSize(layout.Size);
            ImGui.SetNextWindowSizeConstraints(layout.SizeConstraintMin, layout.SizeConstraintMax);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, GuiTheme.ConsoleBgColor);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 6f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);

            ImGui.Begin("cli"u8, ConsoleWindowFlags);
            {
                ImGui.PushStyleColor(ImGuiCol.Text, 0x99FFFFFF);
                ImGui.SeparatorText("Console"u8);
                ImGui.PopStyleColor();
            }
            ImGui.End();

            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
        }
    }

    private void DrawLeftSidebarHeader()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));

        if (!ImGui.BeginTabBar("##panel-tabs"u8, ImGuiTabBarFlags.FittingPolicyShrink))
        {
            ImGui.PopStyleVar();
            return;
        }

        var leftPanelId = stateContext.State.LeftPanelId;

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
        ImGui.PopStyleVar();
    }


    private void DrawTopbar(float width, FrameContext ctx)
    {
        var state = stateContext.State;
        var isMetrics = stateContext.IsMetricMode();

        var size = new Vector2(GuiTheme.TopbarHeight);

        GuiTheme.PushFontIconLarge();

        if (ImGui.Selectable(ref ctx.Sw.Write(IconNames.Activity), isMetrics, 0, size))
        {
            stateContext.EmitTransition(new TransitionMessage { Clear = true });
            stateContext.EmitTransition(TransitionMessage.PushLeft(PanelId.MetricsLeft));
            stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.MetricsRight));
        }

        ImGui.SameLine();

        if (ImGui.Selectable(ref ctx.Sw.Write(IconNames.LayoutGrid), !isMetrics, 0, size))
            stateContext.EmitTransition(new TransitionMessage { Clear = true });

        //
        ImGui.SameLine(width - (size.X * 5) - GuiTheme.WindowPadding.X * 2 - 22.0f);
        //

        var hasSelection = stateContext.Selection.HasSelection();
        var propertyFlag = hasSelection ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled;

        if (ImGui.Selectable(ref ctx.Sw.Write(IconNames.MousePointer2), hasSelection, propertyFlag, size))
            stateContext.EmitTransition(new TransitionMessage { Clear = true });

        ImGui.SameLine();
        if (ImGui.Selectable(ref ctx.Sw.Write(IconNames.Video), state.RightPanelId == PanelId.Camera, 0, size))
            stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.Camera));

        ImGui.SameLine();
        if (ImGui.Selectable(ref ctx.Sw.Write(IconNames.Sun), state.RightPanelId == PanelId.Lighting, 0, size))
            stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.Lighting));

        ImGui.SameLine();
        if (ImGui.Selectable(ref ctx.Sw.Write(IconNames.CloudFog), state.RightPanelId == PanelId.Atmosphere, 0, size))
            stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.Atmosphere));

        ImGui.SameLine();
        if (ImGui.Selectable(ref ctx.Sw.Write(IconNames.Sparkles), state.RightPanelId == PanelId.Visual, 0, size))
            stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.Visual));

        ImGui.PopFont();
    }


    public void CalculatePanelSize()
    {
        var vp = ImGui.GetMainViewport();
        var height = vp.WorkSize.Y - GuiTheme.TopbarHeight;
        var hasLeftSidebar = stateContext.State.LeftPanelId != PanelId.None;
        var leftHeight = hasLeftSidebar ? height : 52;

        var isEditor = stateContext.State.RightPanelId != PanelId.MetricsRight;
        var left = isEditor ? GuiTheme.LeftSidebarDefaultWidth : GuiTheme.LeftSidebarCompactWidth;
        var right = isEditor ? GuiTheme.RightSidebarDefaultWidth : GuiTheme.RightSidebarCompactWidth;

        scoped ref var panelSize = ref _panelSize;
        panelSize.LeftSize = new Vector2(left, leftHeight);
        panelSize.LeftPosition = vp.WorkPos with { Y = vp.WorkPos.Y + GuiTheme.TopbarHeight };
        panelSize.RightSize = new Vector2(right, height);
        panelSize.RightPosition =
            new Vector2(vp.WorkPos.X + vp.WorkSize.X - right, vp.WorkPos.Y + GuiTheme.TopbarHeight);

        CalculateConsoleSize(in vp, left, right);
    }

    private void CalculateConsoleSize(in ImGuiViewportPtr vp, float leftPanelWidth, float rightPanelWidth)
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

        scoped ref var consoleSize = ref _consoleSize;
        consoleSize.Position = new Vector2(posX, posY);
        consoleSize.Size = new Vector2(targetW, targetH);
        consoleSize.SizeConstraintMin = new Vector2(MathF.Min(minW, centerW), minH);
        consoleSize.SizeConstraintMax =
            new Vector2(MathF.Min(float.Min(maxWCap, centerW), centerW), MathF.Min(maxH, centerH));
    }
}