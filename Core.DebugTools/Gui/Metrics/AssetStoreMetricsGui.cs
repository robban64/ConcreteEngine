using System.Numerics;
using Core.DebugTools.Data;
using ImGuiNET;
using static Core.DebugTools.Utils.GuiUtils;

namespace Core.DebugTools.Gui.Metrics;

public static class AssetStoreMetricsGui
{
    public static void DrawAssetStoreMetrics(MetricReport report)
    {
        DrawSectionHeader("Asset Metrics");

        if (!string.IsNullOrEmpty(report.MaterialMetrics))
        {
            ImGui.PushTextWrapPos(0f);
            ImGui.TextDisabled(report.MaterialMetrics);
            ImGui.PopTextWrapPos();
            ImGui.Dummy(new Vector2(0, 4));
        }

        if (report.AssetMetrics.Count == 0)
        {
            ImGui.TextDisabled("No asset metas");
            return;
        }

        const ImGuiTableFlags flags =
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit;

        if (!ImGui.BeginTable("asset_store_tbl", 3, flags)) return;
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthStretch, 1.00f);
        ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthStretch, 0.35f);
        ImGui.TableSetupColumn("Files", ImGuiTableColumnFlags.WidthStretch, 0.35f);
        ImGui.TableHeadersRow();

        foreach (var it in report.AssetMetrics)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(it.Name);

            ImGui.TableSetColumnIndex(1);
            RightAlignCellText(it.Assets);

            ImGui.TableSetColumnIndex(2);
            RightAlignCellText(it.AssetFiles);
        }

        ImGui.EndTable();
    }
}