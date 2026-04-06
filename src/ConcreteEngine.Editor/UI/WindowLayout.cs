using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal static class WindowLayout
{
    private const ImGuiWindowFlags SidebarFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

    private const ImGuiWindowFlags TopbarFlags = SidebarFlags | ImGuiWindowFlags.NoScrollbar;

    private const ImGuiWindowFlags ConsoleWindowFlags =
        ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus |
        ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoTitleBar |
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

    public static Vector2 ActiveWindowSize = Vector2.Zero;
    public static Vector2 ActiveWindowPos = Vector2.Zero;
    public static ImDrawListPtr ActiveDrawList = ImDrawListPtr.Null;

    private static PanelSize _windowSizes;
    private static ConsoleWindowSize _consoleSize;
    private static readonly Vector2 ConsolePadding = new(12f, 6f);
    private static readonly Vector2 SidebarTabFramePadding = new(12f, 4f);
    private static readonly Vector2 RightWindowPadding = GuiTheme.WindowPadding with { X = 12 };

    private static AvgFrameTimer avg;

    public static void DrawPanels(PanelState panels, StateContext stateContext, FrameContext ctx)
    {
        ref readonly var panelSize = ref _windowSizes;
        ImGui.SetNextWindowPos(panelSize.LeftPosition);
        ImGui.SetNextWindowSize(panelSize.LeftSize);
        if (ImGui.Begin("left-sidebar"u8, SidebarFlags))
        {
            ActiveWindowSize = panelSize.LeftSize;
            ActiveWindowPos = panelSize.LeftPosition;
            ActiveDrawList = ImGui.GetWindowDrawList();

            DrawLeftSidebarHeader(stateContext);
            ImGui.PushID((int)panels.LeftPanelId);
            avg.BeginSample();
            panels.Left.OnDraw(ctx);
            if (avg.EndSample() > 60) avg.ResetAndPrint();
            ImGui.PopID();
        }

        ImGui.End();

        ImGui.SetNextWindowPos(panelSize.RightPosition);
        ImGui.SetNextWindowSize(panelSize.RightSize);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, RightWindowPadding);
        if (ImGui.Begin("right-sidebar"u8, SidebarFlags))
        {
            ActiveWindowSize = panelSize.RightSize;
            ActiveWindowPos = panelSize.RightPosition;
            ActiveDrawList = ImGui.GetWindowDrawList();

            ImGui.PushID((int)panels.RightPanelId);
            panels.Right.OnDraw(ctx);
            ImGui.PopID();
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }

    public static void DrawConsole(PanelState panels)
    {
        ref readonly var layout = ref _consoleSize;
        ImGui.SetNextWindowPos(layout.Position);
        ImGui.SetNextWindowSize(layout.Size);
        ImGui.SetNextWindowSizeConstraints(layout.SizeConstraintMin, layout.SizeConstraintMax);

        ImGui.PushStyleColor(ImGuiCol.WindowBg, ConsolePanel.ConsoleBgColor);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, ConsolePadding);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);

        if (ImGui.Begin("cli"u8, ConsoleWindowFlags))
        {
            ActiveWindowSize = layout.Size;
            ActiveWindowPos = layout.Position;
            ActiveDrawList = ImGui.GetWindowDrawList();

            panels.ConsoleUi.Draw();
        }

        ImGui.End();

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor();
    }

    public static void DrawTopbar(Topbar topbar)
    {
        var size = new Vector2(ImGuiSystem.OutputSize.Width, GuiTheme.TopbarHeight);
        ImGui.SetNextWindowSize(size);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        if (ImGui.Begin("topbar"u8, TopbarFlags))
        {
            ActiveWindowSize = size;
            ActiveWindowPos = default;
            ActiveDrawList = ImGui.GetWindowDrawList();
            topbar.Draw(size.X);
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }

    private static void DrawLeftSidebarHeader(StateContext stateContext)
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


    public static void CalculatePanelSize(PanelId leftPanelId, PanelId rightPanelId)
    {
        var vp = ImGui.GetMainViewport();
        var height = vp.WorkSize.Y - GuiTheme.TopbarHeight;
        var hasLeftSidebar = leftPanelId != PanelId.None;
        var leftHeight = hasLeftSidebar ? height : 52;

        var isEditor = rightPanelId != PanelId.MetricsRight;
        var left = isEditor ? GuiTheme.LeftSidebarDefaultWidth : GuiTheme.LeftSidebarCompactWidth;
        var right = isEditor ? GuiTheme.RightSidebarDefaultWidth : GuiTheme.RightSidebarCompactWidth;

        ref var panelSize = ref _windowSizes;
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