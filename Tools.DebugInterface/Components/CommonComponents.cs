#region

using System.Numerics;
using ImGuiNET;

#endregion

namespace Tools.DebugInterface.Components;

internal sealed class CommonComponents
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

    public static void MetricLine(string? text)
    {
        if (!string.IsNullOrEmpty(text))
            ImGui.TextUnformatted(text);
    }

    public static void RightAlignCellText(string? text)
    {
        var s = text ?? "";
        var avail = ImGui.GetContentRegionAvail().X;
        var w = ImGui.CalcTextSize(s).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, avail - w));
        ImGui.TextUnformatted(s);
    }
}