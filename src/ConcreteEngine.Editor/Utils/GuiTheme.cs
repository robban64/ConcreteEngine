using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Utils;

internal static class GuiTheme
{
    public static Vector2 WindowPadding = new(8f, 8f);
    public static Vector2 FramePadding = new(4f, 4f);

    public static Vector2 ItemSpacing = new(8f, 6f);
    public static Vector2 ItemInnerSpacing = new(6, 4);
    public static float IndentSpacing = 20.0f;

    public static Vector4 ConsoleBgColor = new(0.08f, 0.08f, 0.08f, 0.94f);
    public static Vector4 ConsoleInnerBgColor = new(0.10f, 0.10f, 0.10f, 0.75f);

    public const int TopbarHeight = 44;
    public const float PanelOpacity = 0.95f;

    public const int LeftSidebarDefaultWidth = 264;
    public const int LeftSidebarCompactWidth = 220;

    public const int RightSidebarCompactWidth = 230;
    public const int RightSidebarDefaultWidth = 258; //248;


    public static readonly Vector4 PrimaryColor = new(0.00f, 0.47f, 0.76f, 1.00f);
    public static readonly Vector4 SelectedColor = new(0.18f, 0.64f, 0.95f, 1.00f);
    public static readonly Vector4 Blue1 = Color4.FromRgba(77, 174, 225);
    public static readonly Vector4 Blue2 = Color4.FromRgba(128, 195, 233);

    public static void SetTheme(float scale)
    {
        var style = ImGui.GetStyle();
        var colors = style.Colors;

        style.ScaleAllSizes(1);

        style.TabRounding = 0.5f;
        style.TabBarBorderSize = 1f;
        style.TabBorderSize = 1f;

        style.FrameBorderSize = 0.0f;

        style.ScrollbarSize = 14.0f;

        style.WindowPadding = WindowPadding;
        style.ItemSpacing = ItemSpacing;
        style.FramePadding = FramePadding;
        style.ItemInnerSpacing = ItemInnerSpacing;
        style.IndentSpacing = IndentSpacing;

        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.20f, 0.22f, 0.27f, 1.00f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.24f, 0.26f, 0.31f, 1.00f);
        colors[(int)ImGuiCol.FrameBgActive] = PrimaryColor;
        colors[(int)ImGuiCol.TextSelectedBg] = PrimaryColor with { W = 0.35f };
        // Colors
        colors[(int)ImGuiCol.Header] = PrimaryColor;
        colors[(int)ImGuiCol.HeaderHovered] = Blue1;
        colors[(int)ImGuiCol.HeaderActive] = SelectedColor;

        colors[(int)ImGuiCol.Tab] = PrimaryColor;
        colors[(int)ImGuiCol.TabHovered] = Blue1;
        colors[(int)ImGuiCol.TabSelected] = SelectedColor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushTheme(bool leftExpanded, bool rightExpanded)
    {
    }
}