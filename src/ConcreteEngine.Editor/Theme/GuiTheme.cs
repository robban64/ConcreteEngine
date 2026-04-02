using System.Numerics;
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

    public const ImGuiInputTextFlags InputNameFlags =
        ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsNoBlank |
        ImGuiInputTextFlags.CallbackCharFilter;


    public const float TopbarHeight = 44;

    public const float PanelOpacity = 0.95f;

    public const float FormItemWidth = 220;
    public const float FormItemInlineWidth = 160;

    public const float LeftSidebarDefaultWidth = 264;
    public const float LeftSidebarCompactWidth = 242;

    public const float RightSidebarDefaultWidth = 264;
    public const float RightSidebarCompactWidth = 210;

    public static readonly Vector2 WindowPadding = new(6f, 6f);
    public static readonly Vector2 FramePadding = new(5f, 3f);

    public static readonly Vector2 CellPadding = new(6f, 6f);
    public static readonly Vector2 ItemSpacing = new(6f, 6f);
    public static readonly Vector2 ItemInnerSpacing = new(6f, 6f);

    public static readonly float IndentSpacing = 20.0f;

    public static ImFontPtr TextFont;
    public static ImFontPtr IconFont;

    public const float FontSizeDefault = 14.0f;

    public const float IconSizeMedium = 18.0f;
    public const float IconSizeLarge = 22.0f;

    public static void PushFontText() => ImGui.PushFont(TextFont, FontSizeDefault);

    public static void PushFontIconMedium() => ImGui.PushFont(IconFont, IconSizeMedium);

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
        style.CellPadding = CellPadding;
        style.ItemSpacing = ItemSpacing;
        style.FramePadding = FramePadding;
        style.ItemInnerSpacing = ItemInnerSpacing;
        style.IndentSpacing = IndentSpacing;

        colors[(int)ImGuiCol.Button] = new Color4(0.20f, 0.25f, 0.29f);
        colors[(int)ImGuiCol.ButtonHovered] = new Color4(0.28f, 0.56f, 1.00f);
        colors[(int)ImGuiCol.ButtonActive] = new Color4(0.06f, 0.53f, 0.98f);

        colors[(int)ImGuiCol.Header] = new Color4(0.20f, 0.25f, 0.29f);
        colors[(int)ImGuiCol.HeaderHovered] = new Color4(0.12f, 0.20f, 0.28f);
        colors[(int)ImGuiCol.HeaderActive] = new Color4(0.09f, 0.12f, 0.14f);

        colors[(int)ImGuiCol.Tab] = PrimaryColor;
        colors[(int)ImGuiCol.TabHovered] = HoverColor;
        colors[(int)ImGuiCol.TabSelected] = SelectedColor;

        colors[(int)ImGuiCol.Separator] = new Color4(0.43f, 0.43f, 0.50f, 0.50f);
        colors[(int)ImGuiCol.SeparatorHovered] = new Color4(0.72f, 0.72f, 0.72f, 0.78f);
        colors[(int)ImGuiCol.SeparatorActive] = new Color4(0.51f, 0.51f, 0.51f);

        colors[(int)ImGuiCol.WindowBg].W = PanelOpacity;
        colors[(int)ImGuiCol.Text] = TextPrimary;
        colors[(int)ImGuiCol.TextDisabled] = TextDisabled;
        colors[(int)ImGuiCol.TextLink] = PrimaryColor;

        colors[(int)ImGuiCol.FrameBg] = new Color4(0.20f, 0.25f, 0.29f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Color4(0.12f, 0.20f, 0.28f);
        colors[(int)ImGuiCol.FrameBgActive] = new Color4(0.09f, 0.12f, 0.14f);
        colors[(int)ImGuiCol.TextSelectedBg] = PrimaryColor with { A = 0.35f };
/*
        colors[(int)ImGuiCol.Header] = PrimaryColor;
        colors[(int)ImGuiCol.HeaderHovered] = HoverColor;
        colors[(int)ImGuiCol.HeaderActive] = SelectedColor;

        colors[(int)ImGuiCol.Button] = PrimaryColor;
        colors[(int)ImGuiCol.ButtonHovered] = HoverColor;
        colors[(int)ImGuiCol.ButtonActive] = SelectedColor;


        */
    }
}