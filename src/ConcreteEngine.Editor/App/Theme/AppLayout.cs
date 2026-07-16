using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.App.Theme.GuiTheme;

namespace ConcreteEngine.Editor.App.Theme;

internal static class AppLayout
{
    public static ImFontPtr TextFont;
    public static ImFontPtr IconFont;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushFontTextMedium() => ImGui.PushFont(TextFont, FontSizeMedium);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushFontText() => ImGui.PushFont(TextFont, FontSizeDefault);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushFontTextSmall() => ImGui.PushFont(TextFont, FontSizeSmall);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushFontIcon() => ImGui.PushFont(IconFont, FontSizeLarge);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushFontIconLarge() => ImGui.PushFont(IconFont, FontSizeXl);

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetFrameHeightWithSpacing(float fontSize = FontSizeDefault) =>
        fontSize + FramePadding.Y * 2 + ItemSpacing.Y;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetRowWidthForItems(int itemCount) =>
        (ImGui.GetContentRegionAvail().X - ItemSpacing.X) / itemCount;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NextAlignTextVertical(float rowHeight, float fontSize)
    {
        if (rowHeight == 0) return;
        var yOffset = (rowHeight - fontSize) * 0.5f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float NextAlignTextVerticalTop(float top, float rowHeight, float fontSize = FontSizeDefault)
    {
        if (rowHeight == 0) return 0;
        var yOffset = (rowHeight - fontSize) * 0.5f;
        ImGui.SetCursorPosY(top + yOffset);
        return top + yOffset;
    }
}