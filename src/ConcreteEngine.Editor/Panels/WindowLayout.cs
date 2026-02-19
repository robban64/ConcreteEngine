using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Widgets;
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

    private const int TopBtnWidth = 74;

    private enum LeftSidebarTabs : byte { Asset, Scene }

    private PanelSize _panelSize;
    private ConsoleWindowSize _consoleSize;

    private readonly EnumTabBar<LeftSidebarTabs> _leftTabBar = new();

    public void Draw(in FrameContext ctx)
    {
        // top
        var vp = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(vp.WorkPos);
        ImGui.SetNextWindowSize(vp.Size with { Y = GuiTheme.TopbarHeight });
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        ImGui.Begin("topbar"u8, GuiTheme.TopbarFlags);
        {
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));
            ImGui.PushStyleColor(ImGuiCol.Text, Color4.White);

            DrawModeSelector(vp.Size.X, in ctx);

            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
        }
        ImGui.End();
        ImGui.PopStyleVar();

        DrawSidebars();
        DrawConsoleWindow();
    }

    private void DrawSidebars()
    {
        // left
        scoped ref readonly var panelSize = ref _panelSize;
        ImGui.SetNextWindowPos(panelSize.LeftPosition);
        ImGui.SetNextWindowSize(panelSize.LeftSize);
        ImGui.Begin("left-sidebar"u8, GuiTheme.SidebarFlags);
        if (!stateContext.IsMetricMode() && _leftTabBar.Draw(out var selected, ImGuiTabBarFlags.FittingPolicyShrink))
        {
            if (selected == LeftSidebarTabs.Asset)
                stateContext.EmitTransition(TransitionMessage.PushLeft(PanelId.AssetList));
            if (selected == LeftSidebarTabs.Scene)
                stateContext.EmitTransition(TransitionMessage.PushLeft(PanelId.SceneList));
        }

        ImGui.End();

        // right
        ImGui.SetNextWindowPos(panelSize.RightPosition);
        ImGui.SetNextWindowSize(panelSize.RightSize);

        ImGui.Begin("right-sidebar"u8, GuiTheme.SidebarFlags);
        ImGui.End();
    }

    private void DrawConsoleWindow()
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


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawPanels(in FrameContext ctx)
    {
        var panels = stateContext.State;

        DurationProfileTimer.Default.Begin();
        ImGui.Begin("left-sidebar"u8);
        panels.Left.Draw(in ctx);
        ImGui.End();
        DurationProfileTimer.Default.EndPrintSimple();

        ImGui.Begin("right-sidebar"u8);
        panels.Right.Draw(in ctx);
        ImGui.End();
    }

    private void DrawModeSelector(float width,in FrameContext ctx)
    {
        var state = stateContext.State;
        var isMetrics = stateContext.IsMetricMode();
        var sw = ctx.Writer;

        var size = new Vector2(GuiTheme.TopbarHeight);

        ImGui.PushFont(GuiTheme.FontIconMedium, 22.0f);
        
        if (ImGui.Selectable(ref sw.Write(IconNames.Activity), isMetrics, 0, size))
        {
            stateContext.EmitTransition(new TransitionMessage { Clear = true });
            stateContext.EmitTransition(TransitionMessage.PushLeft(PanelId.MetricsLeft));
            stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.MetricsRight));
        }

        ImGui.SameLine();

        if (ImGui.Selectable(ref sw.Write(IconNames.LayoutGrid), !isMetrics, 0, size))
            stateContext.EmitTransition(new TransitionMessage { Clear = true });


        //
        ImGui.SameLine(width - (size.X * 5) - GuiTheme.WindowPadding.X * 2 - 22.0f);
        //

        var hasSelection = stateContext.Selection.HasSelection();
        var propertyFlag = hasSelection ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled;
        if (ImGui.Selectable(ref sw.Write(IconNames.MousePointer2), hasSelection, propertyFlag, size))
            stateContext.EmitTransition(new TransitionMessage { Clear = true });

        ImGui.SameLine();
        if (ImGui.Selectable(ref sw.Write(IconNames.Video), state.RightPanelId == PanelId.World, 0, size))
            stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.World));
        
        ImGui.SameLine();
        if (ImGui.Selectable(ref sw.Write(IconNames.Mountain), state.RightPanelId == PanelId.World, 0, size))
            stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.World));
    
        ImGui.SameLine();
        if (ImGui.Selectable(ref sw.Write(IconNames.CloudSun), state.RightPanelId == PanelId.World, 0, size))
            stateContext.EmitTransition(TransitionMessage.PushRight(PanelId.World));

        ImGui.SameLine();

        if (ImGui.Selectable(ref sw.Write(IconNames.Sparkles), state.RightPanelId == PanelId.Visual, 0, size))
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

        ref var panelSize = ref _panelSize;
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

        ref var consoleSize = ref _consoleSize;
        consoleSize.Position = new Vector2(posX, posY);
        consoleSize.Size = new Vector2(targetW, targetH);
        consoleSize.SizeConstraintMin = new Vector2(MathF.Min(minW, centerW), minH);
        consoleSize.SizeConstraintMax =
            new Vector2(MathF.Min(float.Min(maxWCap, centerW), centerW), MathF.Min(maxH, centerH));
    }
}