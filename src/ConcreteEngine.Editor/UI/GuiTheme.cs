using System.Numerics;
using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.UI.Palette;

namespace ConcreteEngine.Editor.UI;

internal static class GuiTheme
{
    public const ImGuiWindowFlags SidebarFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

    public const ImGuiWindowFlags TopbarFlags = SidebarFlags | ImGuiWindowFlags.NoScrollbar;

    public const ImGuiTableFlags TableFlags =
        ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoBordersInBody |
        ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit;


    public const float TopbarHeight = 44;
    public const float ListRowHeight = 32;
    public const float ListPaddedRowHeight = 32 + 4;
    public const float IdColWidth = 36;

    public const float PanelOpacity = 0.95f;

    public const float LeftSidebarDefaultWidth = 264;
    public const float LeftSidebarCompactWidth = 242;

    public const float RightSidebarDefaultWidth = 258;
    public const float RightSidebarCompactWidth = 210;

    public static readonly Vector2 WindowPadding = new(6f, 4f);
    public static readonly Vector2 FramePadding = new(6f, 4f);

    public static readonly Vector2 ItemSpacing = new(6f, 4f);
    public static readonly Vector2 ItemInnerSpacing = new(6f, 4f);
    public static readonly float IndentSpacing = 20.0f;

    public static readonly Vector4 ConsoleBgColor = new(0.08f, 0.08f, 0.08f, 0.94f);
    public static readonly Vector4 ConsoleInnerBgColor = new(0.10f, 0.10f, 0.10f, 0.75f);

    public static ImFontPtr TextFont;
    public static ImFontPtr FontIconMedium;

    public const float TextFontSize = 14.0f;
    public const float IconMediumSize = 18.0f;
    public const float IconLargeSize = 22.0f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushFontText() => ImGui.PushFont(TextFont, TextFontSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushFontIconText() => ImGui.PushFont(FontIconMedium, TextFontSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushFontIconMedium() => ImGui.PushFont(FontIconMedium, IconMediumSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushFontIconLarge() => ImGui.PushFont(FontIconMedium, IconLargeSize);

    public static void SetTheme(float scale)
    {
        var style = ImGui.GetStyle();
        var colors = style.Colors;

        style.ScaleAllSizes(scale);

        style.WindowRounding = 2f;
        style.ChildRounding = 2f;
        style.PopupRounding = 2f;

        style.FrameRounding = 1.5f;
        style.TabRounding = 1.5f;

        style.TabBarBorderSize = 1f;
        style.TabBorderSize = 1f;

        style.FrameBorderSize = 0.0f;

        style.ScrollbarSize = 14.0f;

        style.GrabRounding = 3f;

        style.WindowPadding = WindowPadding;
        style.ItemSpacing = ItemSpacing;
        style.FramePadding = FramePadding;
        style.ItemInnerSpacing = ItemInnerSpacing;
        style.IndentSpacing = IndentSpacing;


        colors[(int)ImGuiCol.WindowBg].W = PanelOpacity;
        colors[(int)ImGuiCol.Text] = TextPrimary;
        colors[(int)ImGuiCol.TextDisabled] = TextDisabled;
        colors[(int)ImGuiCol.TextLink] = PrimaryColor;

        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.20f, 0.22f, 0.27f, 1.00f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.24f, 0.26f, 0.31f, 1.00f);
        colors[(int)ImGuiCol.FrameBgActive] = PrimaryColor;
        colors[(int)ImGuiCol.TextSelectedBg] = PrimaryColor with { A = 0.35f };

        colors[(int)ImGuiCol.Header] = PrimaryColor;
        colors[(int)ImGuiCol.HeaderHovered] = HoverColor;
        colors[(int)ImGuiCol.HeaderActive] = SelectedColor;

        colors[(int)ImGuiCol.Button] = PrimaryColor;
        colors[(int)ImGuiCol.ButtonHovered] = HoverColor;
        colors[(int)ImGuiCol.ButtonActive] = SelectedColor;


        colors[(int)ImGuiCol.Tab] = PrimaryColor;
        colors[(int)ImGuiCol.TabHovered] = HoverColor;
        colors[(int)ImGuiCol.TabSelected] = SelectedColor;
    }
}