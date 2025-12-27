using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ImGuiNET;

namespace ConcreteEngine.Editor.Utils;

internal static class GuiTheme
{
    public static Vector4 ConsoleBgColor = new(0.08f, 0.08f, 0.08f, 0.94f);
    public static Vector4 ConsoleInnerBgColor = new(0.10f, 0.10f, 0.10f, 0.75f);

    public const int TopbarHeight = 44;
    public const float PanelOpacity = 0.95f;

    public const int LeftSidebarWidth = 280;
    public const int RightSidebarCompactWidth = 160;
    public const int RightSidebarExpandedWidth = 280; //248;

    public static bool RightSidebarExpanded { get; set; } = false;
    public static int RightSidebarWidth => RightSidebarExpanded ? RightSidebarExpandedWidth : RightSidebarCompactWidth;


    public static readonly Vector4 PrimaryColor = Color4.FromRgba(0, 121, 193).AsVec4();
    public static readonly Vector4 SelectedColor = Color4.FromRgba(46, 163, 242).AsVec4();

    public static readonly Vector4 Blue1 = Color4.FromRgba(77, 174, 225).AsVec4();
    public static readonly Vector4 Blue2 = Color4.FromRgba(128, 195, 233).AsVec4();

    //public static readonly Vector4 BlueSecondary = Color4.FromRgba(33, 116, 166).AsVec4();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushTheme(bool sideBarExpanded)
    {
        RightSidebarExpanded = sideBarExpanded;

        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Blue1);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, SelectedColor);
        ImGui.PushStyleColor(ImGuiCol.Header, PrimaryColor);

        ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 0.5f);
        ImGui.PushStyleVar(ImGuiStyleVar.TabBarBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.TabBorderSize, 1);
        ImGui.PushStyleColor(ImGuiCol.TabHovered, Blue1);
        ImGui.PushStyleColor(ImGuiCol.TabActive, SelectedColor);
        ImGui.PushStyleColor(ImGuiCol.Tab, PrimaryColor);
    }

    public static void PopTheme()
    {
        ImGui.PopStyleColor(6);
        ImGui.PopStyleVar(3);
    }
}