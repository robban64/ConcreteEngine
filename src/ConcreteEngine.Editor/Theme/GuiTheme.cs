using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using static ConcreteEngine.Editor.Theme.Palette;

namespace ConcreteEngine.Editor.Theme;

internal static class GuiTheme
{
    public const ImGuiTableFlags TableFlags =
        ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoBordersInBody |
        ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit;

    public const ImGuiTableFlags ListTableFlags =
        ImGuiTableFlags.ScrollY |
        ImGuiTableFlags.NoPadOuterX |
        ImGuiTableFlags.NoPadInnerX |
        ImGuiTableFlags.SizingFixedFit;

    public const ImGuiInputTextFlags InputNameFlags =
        ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsNoBlank |
        ImGuiInputTextFlags.CallbackCharFilter;


    public const float TopbarHeight = 36f;
    public const float MenuBarHeight = 30f;
    public const float TopOffset = TopbarHeight + MenuBarHeight;

    public const float FontSizeDefault = 14.0f;
    public const float IconSizeMedium = 18.0f;
    public const float IconSizeLarge = 24.0f;
    public const float FormItemWidth = 220;
    public const float FormItemInlineWidth = 160;

    public const float IndentSpacing = 20.0f;

    public static readonly Vector2 WindowPadding = new(12f, 6f);

    public static readonly Vector2 FramePadding = new(5f, 3f);
    public static readonly Vector2 MenuFramePadding = new(5f, 8f);

    public static readonly Vector2 CellPadding = new(6f, 6f);
    public static readonly Vector2 ItemSpacing = new(6f, 6f);
    public static readonly Vector2 ItemInnerSpacing = new(6f, 6f);

    public static ImFontPtr TextFont;
    public static ImFontPtr IconFont;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushFontText() => ImGui.PushFont(TextFont, FontSizeDefault);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushFontIcon() => ImGui.PushFont(IconFont, IconSizeMedium);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushFontIconLarge() => ImGui.PushFont(IconFont, IconSizeLarge);

    public static void SetImGuizmoTheme()
    {
        var style = ImGuizmo.GetStyle();

        ImGuizmo.SetGizmoSizeClipSpace(0.133f);
        style.TranslationLineThickness = 4f;
        style.TranslationLineArrowSize = 8f;
        style.RotationLineThickness = 3f;
        style.RotationOuterLineThickness = 4f;
        style.ScaleLineThickness = 4f;
        style.ScaleLineCircleSize = 8f;
        style.HatchedAxisLineThickness = 6f;
        style.CenterCircleSize = 6f;
        style.HatchedAxisLineThickness = 0;

        style.Colors[(int)ImGuizmoColor.Selection] = new Vector4(1f, 0.8f, 0f, 1f);

        style.Colors[(int)ImGuizmoColor.DirectionX] = RedBase;
        style.Colors[(int)ImGuizmoColor.DirectionY] = BlueBase;
        style.Colors[(int)ImGuizmoColor.DirectionZ] = GreenBase;

        style.Colors[(int)ImGuizmoColor.PlaneX] = RedBase;
        style.Colors[(int)ImGuizmoColor.PlaneY] = BlueBase;
        style.Colors[(int)ImGuizmoColor.PlaneZ] = GreenBase;
    }

    public static void SetTheme(float scale)
    {
        var style = ImGui.GetStyle();
        var colors = style.Colors;

        style.ScaleAllSizes(scale);

        style.WindowRounding = 0;
        style.ChildRounding = 0;
        style.PopupRounding = 2f;

        style.FrameRounding = 0;
        style.TabRounding = 0;

        style.TabBarBorderSize = 1f;
        style.TabBorderSize = 1f;

        style.FrameBorderSize = 0.0f;

        style.ScrollbarSize = 14.0f;

        style.GrabRounding = 3f;

        style.WindowPadding = WindowPadding;
        style.CellPadding = CellPadding;
        style.ItemSpacing = ItemSpacing;
        style.FramePadding = FramePadding;
        style.ItemInnerSpacing = ItemInnerSpacing;
        style.IndentSpacing = IndentSpacing;

        colors[(int)ImGuiCol.FrameBg] = FrameBg;
        colors[(int)ImGuiCol.FrameBgHovered] = FrameBgHovered;
        colors[(int)ImGuiCol.FrameBgActive] = FrameBgActive;

        colors[(int)ImGuiCol.WindowBg] = BgColor;
        colors[(int)ImGuiCol.ChildBg] = new Color4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int)ImGuiCol.PopupBg] = SurfaceDark;

        colors[(int)ImGuiCol.MenuBarBg] = new Color4(0.10f, 0.10f, 0.11f);

        colors[(int)ImGuiCol.ScrollbarBg] = default;
        colors[(int)ImGuiCol.ScrollbarGrab] = new Color4(0.25f, 0.25f, 0.25f, 0.80f);
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Color4(0.35f, 0.35f, 0.35f, 0.80f);
        colors[(int)ImGuiCol.ScrollbarGrabActive] = new Color4(0.45f, 0.45f, 0.45f, 0.80f);
        colors[(int)ImGuiCol.Border] = new Color4(0.25f, 0.25f, 0.25f, 0.30f);

        colors[(int)ImGuiCol.Button] = FrameBg;
        colors[(int)ImGuiCol.ButtonHovered] = PrimaryColor;
        colors[(int)ImGuiCol.ButtonActive] = ActiveColor;

        colors[(int)ImGuiCol.Header] = new Color4(0.23f, 0.26f, 0.29f);
        colors[(int)ImGuiCol.HeaderHovered] = FrameBgHovered;
        colors[(int)ImGuiCol.HeaderActive] = FrameBgActive;

        colors[(int)ImGuiCol.Tab] = PrimaryColor;
        colors[(int)ImGuiCol.TabHovered] = HoverColor;
        colors[(int)ImGuiCol.TabSelected] = ActiveColor;

        colors[(int)ImGuiCol.Separator] = new Color4(0.25f, 0.25f, 0.25f, 0.60f);
        colors[(int)ImGuiCol.SeparatorHovered] = new Color4(0.40f, 0.40f, 0.40f);
        colors[(int)ImGuiCol.SeparatorActive] = PrimaryColor;

        colors[(int)ImGuiCol.Text] = TextPrimary;
        colors[(int)ImGuiCol.TextDisabled] = TextDisabled;
        colors[(int)ImGuiCol.TextLink] = PrimaryColor;
        colors[(int)ImGuiCol.TextSelectedBg] = PrimaryColor with { A = 0.35f };
    }
}