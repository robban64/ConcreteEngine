using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ImGuiNET;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.Utils;

internal static class GuiUtils
{
    public static (int, int) ItemActivatedAndDeactivatedAfterEdit(int idx)
    {
        return (ImGui.IsItemActive() ? idx : -1, ImGui.IsItemDeactivatedAfterEdit() ? idx : -1);
    }

    public static bool ColumnSelectable(ReadOnlySpan<char> str, bool selected, int colWidth, int colHeight)
    {
        const ImGuiSelectableFlags flags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        var textWidth = ImGui.CalcTextSize(str).X;
        var offset = (colWidth - textWidth) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        return ImGui.Selectable(str, selected, flags, new Vector2(0, colHeight));
    }

    public static void DrawSectionHeader(string title)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, Color4.LightGray);
        ImGui.SeparatorText(title);
        ImGui.PopStyleColor();

        var startX = ImGui.GetCursorScreenPos().X;
        var regionW = ImGui.GetContentRegionAvail().X;

        var dl = ImGui.GetWindowDrawList();
        var p = ImGui.GetItemRectMin();
        var q = new Vector2(startX + regionW, ImGui.GetItemRectMax().Y);
        dl.AddLine(new Vector2(startX, q.Y), q, ImGui.GetColorU32(ImGuiCol.Separator), 1.0f);

        ImGui.Dummy(new Vector2(0, 4));
    }

    public static void MetricText<T>(
        ref ZaSpanStringBuilder za,
        string prefix,
        T value,
        string format = "",
        string suffix = "",
        int space = 50)
        where T : ISpanFormattable
    {
        za.Clear();
        ImGui.TextUnformatted(za.Append(prefix).AsSpan());
        ImGui.SameLine(space);
        za.Clear();
        ImGui.TextUnformatted(za.Append(value, format).Append(suffix).AsSpan());
    }

    public static void MetricHistory(
        ref ZaSpanStringBuilder za,
        string prefix,
        float val1,
        float val2,
        bool hasRef,
        string format = "",
        string suffix = "",
        int space = 50)
    {
        za.Clear();
        ImGui.TextUnformatted(za.Append(prefix).AsSpan());
        ImGui.SameLine(space);
        za.Clear();
        ImGui.TextUnformatted(za.Append(val1, format).Append(suffix).AsSpan());

        if (!hasRef) return;

        float diff = val1 - val2;
        if (Math.Abs(diff) > 0.01f)
        {
            ImGui.SameLine(space * 2);

            string sign = diff > 0 ? "+" : "";
            za.Clear();
            ImGui.TextUnformatted(za.PadLeft("(", 4).Append(sign).Append(diff, format).Append(")").AsSpan());
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CenterAlignTextVertical(ReadOnlySpan<char> text, float rowHeight)
    {
        var fontSize = ImGui.GetFontSize();
        var yOffset = (rowHeight - fontSize) * 0.5f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);
        ImGui.TextUnformatted(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CenterAlignTextHorizontal(ReadOnlySpan<char> text)
    {
        var columnWidth = ImGui.GetColumnWidth();
        var textWidth = ImGui.CalcTextSize(text).X;
        var offset = (columnWidth - textWidth) * 0.5f;

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImGui.TextUnformatted(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CenterAlignText(ReadOnlySpan<char> text, float rowHeight)
    {
        var fontSize = ImGui.GetFontSize();
        var yOffset = (rowHeight - fontSize) * 0.5f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);

        var columnWidth = ImGui.GetColumnWidth();
        var textWidth = ImGui.CalcTextSize(text).X;
        var offset = (columnWidth - textWidth) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        ImGui.TextUnformatted(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RightAlignCellText(ReadOnlySpan<char> text)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        var w = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, avail - w));
        ImGui.TextUnformatted(text);
    }
}