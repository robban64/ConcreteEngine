using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;
using ImGui = Hexa.NET.ImGui.ImGui;

namespace ConcreteEngine.Editor.Core;

internal static class WindowRoot
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

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        ImGui.SetNextWindowSize(WorkSize);
        ImGui.SetNextWindowPos(WorkPosition);

        ImGui.Begin("##Root"u8, null, DockWindowFlags);
        ImGui.PopStyleVar();
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
            var rect = new ViewportRect(ViewportPosition, ViewportSize);
            OnViewport?.Invoke(rect);
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

        uint* dockMainId = nodes, dockLeftId = nodes + 1, dockRightId = nodes + 2, dockBottomId = nodes + 3;

        ImGuiP.DockBuilderSplitNode(*dockMainId, ImGuiDir.Left, 0.20f, dockLeftId, dockMainId);
        ImGuiP.DockBuilderSplitNode(*dockMainId, ImGuiDir.Right, 0.20f, dockRightId, dockMainId);
        ImGuiP.DockBuilderSplitNode(*dockMainId, ImGuiDir.Down, 0.25f, dockBottomId, dockMainId);

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

}