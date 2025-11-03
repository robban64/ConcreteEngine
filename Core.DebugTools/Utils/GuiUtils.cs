#region

using System.Numerics;
using ImGuiNET;

#endregion

namespace Core.DebugTools.Utils;

internal static class GuiUtils
{
    public static void DrawSectionHeader(string title)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, 0x99FFFFFF);
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

    public static void SetupTableColumnId(string name) =>
        ImGui.TableSetupColumn(name, ImGuiTableColumnFlags.WidthFixed, 22.0f);

    public static void CenterAlignCellText(string? text)
    {
        var s = text ?? string.Empty;
        var columnWidth = ImGui.GetColumnWidth();
        var textWidth = ImGui.CalcTextSize(s).X;
        var offset = (columnWidth - textWidth) * 0.5f;

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImGui.TextUnformatted(s);
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