using System.Numerics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Utils;

internal static class GuiTheme
{
    public const int TopbarHeight = 44;
    public const float PanelOpacity = 0.95f;

    public const int LeftSidebarDefaultWidth = 264;
    public const int LeftSidebarCompactWidth = 242;

    public const int RightSidebarDefaultWidth = 258;
    public const int RightSidebarCompactWidth = 210;

    public const ImGuiWindowFlags SidebarFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

    public const ImGuiWindowFlags TopbarFlags = SidebarFlags | ImGuiWindowFlags.NoScrollbar;

    public const ImGuiTableFlags TableFlags =
        ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoBordersInBody |
        ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingFixedFit;


    public static readonly Vector2 WindowPadding = new(8f, 8f);
    public static readonly Vector2 FramePadding = new(4f, 4f);

    public static readonly Vector2 ItemSpacing = new(8f, 6f);
    public static readonly Vector2 ItemInnerSpacing = new(6, 4);
    public static readonly float IndentSpacing = 20.0f;


    public static readonly Vector4 PrimaryColor = new(0.00f, 0.47f, 0.76f, 1.00f);
    public static readonly Vector4 SelectedColor = new(0.18f, 0.64f, 0.95f, 1.00f);
    public static readonly Vector4 Blue1 = new(0.3f, 0.68f, 0.88f, 1f);
    public static readonly Vector4 Blue2 = new(0.5f, 0.76f, 0.91f, 1f);

    public static readonly Vector4 ConsoleBgColor = new(0.08f, 0.08f, 0.08f, 0.94f);
    public static readonly Vector4 ConsoleInnerBgColor = new(0.10f, 0.10f, 0.10f, 0.75f);


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
}