using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ImGuiNET;

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
        ImGui.PushStyleColor(ImGuiCol.Text, Color4.LightGray.AsVec4());
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

    public static void TextIfNotNull(string? text)
    {
        if (!string.IsNullOrEmpty(text))
            ImGui.TextUnformatted(text);
    }

    public static void CenterAlignTextVertical(ReadOnlySpan<char> text, float rowHeight)
    {
        var fontSize = ImGui.GetFontSize();
        var yOffset = (rowHeight - fontSize) * 0.5f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);
        ImGui.TextUnformatted(text);
    }

    public static void CenterAlignTextHorizontal(ReadOnlySpan<char> text)
    {
        var columnWidth = ImGui.GetColumnWidth();
        var textWidth = ImGui.CalcTextSize(text).X;
        var offset = (columnWidth - textWidth) * 0.5f;

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImGui.TextUnformatted(text);
    }

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

    public static void RightAlignCellText(string? text)
    {
        var s = text ?? string.Empty;
        var avail = ImGui.GetContentRegionAvail().X;
        var w = ImGui.CalcTextSize(s).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, avail - w));
        ImGui.TextUnformatted(s);
    }
}