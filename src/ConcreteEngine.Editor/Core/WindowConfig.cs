using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.ImGuiSystem;
using ImGui = Hexa.NET.ImGui.ImGui;

namespace ConcreteEngine.Editor.Theme;

internal static class WindowConfig
{
    private const ImGuiWindowFlags DockWindowFlags =
        ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse |
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus |
        ImGuiWindowFlags.NoNavFocus;

    public static bool HasDockSpace { get; private set; }
    public static uint DockSpaceId { get; private set; }
    public static uint ViewportId { get; private set; }


    public static Vector2 WorkSize;
    public static Vector2 WorkPosition;

    public static Vector2 ViewportSize;
    public static Vector2 ViewportPosition;

    public static Action<ViewportRect>? OnViewport;
    
    public static ReadOnlySpan<byte> LeftWindowId => "##Left"u8;
    public static ReadOnlySpan<byte> RightWindowId => "##Right"u8;
    public static ReadOnlySpan<byte> BottomWindowId => "##Bottom"u8;
    public static ReadOnlySpan<byte> ViewportWindowId => "##Viewport"u8;
    public static ReadOnlySpan<byte> ToolbarWindowId => "##Toolbar"u8;

    
    public static unsafe void BeginDockSpace()
    {
        const float heightOffset = GuiTheme.TopbarHeight;
        var vp = ImGui.GetMainViewport();

         WorkSize = vp.WorkSize with { Y = vp.WorkSize.Y - heightOffset };
         WorkPosition = vp.WorkPos with { Y = vp.WorkPos.Y + heightOffset };

        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        ImGui.SetNextWindowSize(WorkSize);
        ImGui.SetNextWindowPos(WorkPosition);
        
        ImGui.Begin("##Root"u8, null, DockWindowFlags);
        ImGui.PopStyleVar(2);
        if (!HasDockSpace)
        {
            SetupDock();
        }
        ImGui.DockSpace(DockSpaceId, Vector2.Zero, ImGuiDockNodeFlags.None);
        ImGui.End();

        var node = ImGuiP.DockBuilderGetNode(ViewportId);
        if (node.Size != ViewportSize)
        {
            ViewportSize = node.Size;
            ViewportPosition = node.Pos;
            var rect = new ViewportRect((Vector2I)ViewportPosition, ViewportSize);
            OnViewport!(rect);
        }
    }


    private static unsafe void SetupDock()
    {
        HasDockSpace = true;

        DockSpaceId = ImGui.GetID("MainDockSpace"u8);

        ImGuiP.DockBuilderRemoveNode(DockSpaceId);
        ImGuiP.DockBuilderAddNode(DockSpaceId, ImGuiDockNodeFlags.NoUndocking | ImGuiDockNodeFlags.NoDockingSplit);
        ImGuiP.DockBuilderSetNodeSize(DockSpaceId, WorkSize);

        uint* nodes = stackalloc uint[4];
        nodes[0] = DockSpaceId;

        uint* dockMainId = nodes, dockLeftId = nodes + 1, dockRightId = nodes + 2,  dockBottomId = nodes + 3;

        ImGuiP.DockBuilderSplitNode( *dockMainId, ImGuiDir.Left, 0.20f, dockLeftId, dockMainId);
        ImGuiP.DockBuilderSplitNode( *dockMainId, ImGuiDir.Right, 0.20f, dockRightId, dockMainId);
        ImGuiP.DockBuilderSplitNode( *dockMainId, ImGuiDir.Down, 0.25f, dockBottomId, dockMainId);

        // later version seems to use 2048
        const ImGuiDockNodeFlags noTabBarBit = (ImGuiDockNodeFlags)4096;
        ImGuiP.DockBuilderGetNode(*dockLeftId).LocalFlags |= noTabBarBit;
        ImGuiP.DockBuilderGetNode(*dockRightId).LocalFlags |= noTabBarBit;
        ImGuiP.DockBuilderGetNode(*dockBottomId).LocalFlags |= noTabBarBit;
        ImGuiP.DockBuilderGetNode(*dockMainId).LocalFlags |= noTabBarBit;

        ImGuiP.DockBuilderDockWindow(LeftWindowId, *dockLeftId);
        ImGuiP.DockBuilderDockWindow(RightWindowId, *dockRightId);
        ImGuiP.DockBuilderDockWindow(BottomWindowId, *dockBottomId);
        ImGuiP.DockBuilderDockWindow(ViewportWindowId, *dockMainId);
        ImGuiP.DockBuilderFinish(DockSpaceId);

        ViewportId = *dockMainId;
    }
/*
    public static void CalculateViewport(out ViewportRect viewport)
    {
        const float widthOffset = GuiTheme.SidebarDefaultWidth + GuiTheme.SidebarDefaultWidth;
        const float heightOffset = GuiTheme.TopbarHeight + GuiTheme.MenuBarHeight;

        var pos = new Vector2(GuiTheme.SidebarDefaultWidth, heightOffset);
        var size = new Vector2(OutputSize.Width - widthOffset,
            OutputSize.Height - heightOffset - GuiTheme.BottomDefaultHeight);

        viewport = new ViewportRect((Vector2I)pos, size);
    }

    public static void CalculateLayout(EditorWindowLayout left, EditorWindowLayout right, EditorWindowLayout bottom)
    {
        const float width = GuiTheme.SidebarDefaultWidth;
        const float heightOffset = GuiTheme.TopbarHeight + GuiTheme.MenuBarHeight;

        var outputSize = OutputSize;

        var height = outputSize.Height - heightOffset;
        var rightPos = outputSize.Width - width;

        var size = new Vector2(width, height);
        left.Position = new Vector2(0, heightOffset);
        left.Size = size;
        right.Position = new Vector2(rightPos, heightOffset);
        right.Size = size;

        bottom.Position = new Vector2(size.X, outputSize.Height - GuiTheme.BottomDefaultHeight);
        bottom.Size = new Vector2(rightPos - width, GuiTheme.BottomDefaultHeight);
        //CalculateConsoleLayout(bottom, size.X, size.X);
    }

    private static void CalculateConsoleLayout(EditorWindowLayout layout, float leftW, float rightW)
    {
        const float minW = 400f, maxWCap = 980f;
        const float minH = 240f, maxH = 300f;
        const float margin = 12f;

        var vp = ImGui.GetMainViewport();

        var centerX = vp.WorkPos.X + leftW;
        var centerY = vp.WorkPos.Y;
        var centerW = float.Max(0, vp.WorkSize.X - leftW - rightW);
        var centerH = vp.WorkSize.Y;

        var targetW = float.Clamp(centerW * 0.80f, minW, float.Min(maxWCap, centerW));
        var targetH = float.Clamp(centerH * 0.25f, minH, maxH);

        var posX = centerX + float.Max(0, (centerW - targetW) * 0.5f);
        var posY = centerY + centerH - targetH - margin;

        layout.Position = new Vector2(posX, posY);
        layout.Size = new Vector2(targetW, targetH);
        layout.SizeMin = new Vector2(float.Min(minW, centerW), minH);
        layout.SizeMax = new Vector2(float.Min(float.Min(maxWCap, centerW), centerW), float.Min(maxH, centerH));
    }
*/
/*
    private static void DrawLeftSidebarHeader(StateContext stateContext)
    {
        if (stateContext.IsMetricMode) return;

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, SidebarTabFramePadding);

        if (ImGui.BeginTabBar("##panel-tabs"u8, ImGuiTabBarFlags.FittingPolicyShrink))
        {
            var panelId = WindowManager.GetWindow(WindowId.Left).ActivePanel?.Id ?? PanelId.None;

            if (ImGui.BeginTabItem("Asset"u8))
            {
                if (panelId != PanelId.AssetList)
                    stateContext.EmitTransition(WindowId.Left, PanelId.AssetList);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Scene"u8))
            {
                if (panelId != PanelId.SceneList)
                    stateContext.EmitTransition(WindowId.Left, PanelId.SceneList);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.PopStyleVar();
    }
*/
}