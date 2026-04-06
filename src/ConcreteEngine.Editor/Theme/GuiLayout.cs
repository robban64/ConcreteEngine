using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Theme;

internal static class GuiLayout
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetFrameHeightWithSpacing(float fontSize = GuiTheme.FontSizeDefault)
        => fontSize + GuiTheme.FramePadding.Y * 2 + GuiTheme.ItemSpacing.Y;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetRowWidthForItems(int itemCount) =>
        (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / itemCount;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NextAlignTextVertical(float rowHeight, float fontSize)
    {
        if (rowHeight == 0) return;
        var yOffset = (rowHeight - fontSize) * 0.5f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float NextAlignTextVerticalTop(float top, float rowHeight, float fontSize = GuiTheme.FontSizeDefault)
    {
        if (rowHeight == 0) return 0;
        var yOffset = (rowHeight - fontSize) * 0.5f;
        ImGui.SetCursorPosY(top + yOffset);
        return top + yOffset;
    }

    public static void NextCenterAlignText(ref byte text, float rowHeight)
    {
        if (rowHeight == 0) return;

        var pos = ImGui.GetCursorPos();
        var fontSize = ImGui.GetFontSize();
        var yOffset = (rowHeight - fontSize) * 0.5f;
        ImGui.SetCursorPosY(pos.Y + yOffset);

        var columnWidth = ImGui.GetColumnWidth();
        var textWidth = ImGui.CalcTextSize(ref text).X;
        var offset = (columnWidth - textWidth) * 0.5f;
        ImGui.SetCursorPosX(pos.X + offset);
    }

    public static void NextRightAlignText(ref byte text)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        var w = ImGui.CalcTextSize(ref text).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, avail - w));
    }
}