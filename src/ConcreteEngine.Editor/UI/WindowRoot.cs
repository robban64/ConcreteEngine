using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;
using ImGui = Hexa.NET.ImGui.ImGui;

namespace ConcreteEngine.Editor.UI;

internal static class WindowRoot
{
    private const ImGuiWindowFlags DockWindowFlags =
        ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse |
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus |
        ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoBackground;

    private const ImGuiDockNodeFlags DockNodeFlags =
        ImGuiDockNodeFlags.NoUndocking | ImGuiDockNodeFlags.NoDockingSplit |
        ImGuiDockNodeFlags.PassthruCentralNode;

    public static bool HasDockSpace { get; private set; }
    public static uint DockSpaceId { get; private set; }
    public static uint ViewportId { get; private set; }

    public static Vector2 WorkSize;
    public static Vector2 WorkPosition;

    public static Vector2 ViewportSize;
    public static Vector2 ViewportPosition;

    public static ReadOnlySpan<byte> LeftWindowId => "##Left"u8;
    public static ReadOnlySpan<byte> RightWindowId => "##Right"u8;
    public static ReadOnlySpan<byte> BottomWindowId => "##Bottom"u8;
    public static ReadOnlySpan<byte> ViewportWindowId => "##Viewport"u8;
    public static ReadOnlySpan<byte> ToolbarWindowId => "##Toolbar"u8;


    public static unsafe bool BeginDockSpace()
    {
        var vp = ImGuiSystem.MainViewportPtr;

        WorkSize = vp.WorkSize with { Y = vp.WorkSize.Y - GuiTheme.TopbarHeight };
        WorkPosition = vp.WorkPos with { Y = vp.WorkPos.Y + GuiTheme.TopbarHeight };

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
            return true;
        }

        return false;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void SetupDock()
    {
        HasDockSpace = true;

        float leftWidth = float.Clamp(WorkSize.X * 0.15f, 260f, 320f);
        float rightWidth = float.Clamp(WorkSize.X * 0.15f, 260f, 320f);
        float bottomHeight = float.Clamp(WorkSize.Y * 0.20f, 220f, 300f);

        float leftRatio = leftWidth / WorkSize.X;
        float rightRatio = rightWidth / (WorkSize.X - leftWidth);
        float bottomRatio = bottomHeight / WorkSize.Y;

        DockSpaceId = ImGui.GetID("MainDockSpace"u8);

        ImGuiP.DockBuilderRemoveNode(DockSpaceId);
        ImGuiP.DockBuilderAddNode(DockSpaceId, DockNodeFlags);
        ImGuiP.DockBuilderSetNodeSize(DockSpaceId, WorkSize);

        uint* nodes = stackalloc uint[4];
        nodes[0] = DockSpaceId;
        uint* dockMainId = nodes, dockLeftId = nodes + 1, dockRightId = nodes + 2, dockBottomId = nodes + 3;

        ImGuiP.DockBuilderSplitNode(*dockMainId, ImGuiDir.Left, leftRatio, dockLeftId, dockMainId);
        ImGuiP.DockBuilderSplitNode(*dockMainId, ImGuiDir.Right, rightRatio, dockRightId, dockMainId);
        ImGuiP.DockBuilderSplitNode(*dockMainId, ImGuiDir.Down, bottomRatio, dockBottomId, dockMainId);

        //(ImGuiDockNodeFlags)4096;
        const ImGuiDockNodeFlags noTabBarBit = (ImGuiDockNodeFlags)ImGuiDockNodeFlagsPrivate.NoTabBar;
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