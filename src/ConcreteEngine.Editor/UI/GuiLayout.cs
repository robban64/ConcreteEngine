using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal static class GuiLayout
{
    
    
    public static void NextAlignTextVertical(ReadOnlySpan<byte> text, float rowHeight)
    {
        var fontSize = ImGui.GetFontSize();
        var yOffset = (rowHeight - fontSize) * 0.5f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);
    }

    public static void NextAlignTextHorizontal(ReadOnlySpan<byte> text)
    {
        var columnWidth = ImGui.GetColumnWidth();
        var textWidth = ImGui.CalcTextSize(text).X;
        var offset = (columnWidth - textWidth) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NextCenterAlignText(ReadOnlySpan<byte> text, float rowHeight)
    {
        var pos = ImGui.GetCursorPos();
        var fontSize = ImGui.GetFontSize();
        var yOffset = (rowHeight - fontSize) * 0.5f;
        ImGui.SetCursorPosY(pos.Y + yOffset);

        var columnWidth = ImGui.GetColumnWidth();
        var textWidth = ImGui.CalcTextSize(text).X;
        var offset = (columnWidth - textWidth) * 0.5f;
        ImGui.SetCursorPosX(pos.X + offset);
    }

    public static void NextRightAlignText(ReadOnlySpan<byte> text)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        var w = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, avail - w));
    }

}