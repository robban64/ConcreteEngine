using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Engine.Metadata;
using ImGuiNET;
using ZaString.Core;
using ZaString.Extensions;
using static ConcreteEngine.Editor.Utils.GuiUtils;

namespace ConcreteEngine.Editor.Metrics;


public static class AssetStoreMetricsGui
{
    public static void DrawAssetStoreMetrics()
    {
        ImGui.SeparatorText("Asset Metrics");

        /*
        if (!string.IsNullOrEmpty(report.MaterialMetrics))
        {
            ImGui.PushTextWrapPos(0f);
            ImGui.TextUnformatted(report.MaterialMetrics);
            ImGui.PopTextWrapPos();
            ImGui.Dummy(new Vector2(0, 4));
        }

        if (report.AssetMetrics.Count == 0)
        {
            ImGui.TextDisabled("No asset metas");
            return;
        }*/

        const ImGuiTableFlags flags =
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit;

        if (!ImGui.BeginTable("asset_store_tbl", 3, flags)) return;
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthStretch, 1.00f);
        ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthStretch, 0.35f);
        ImGui.TableSetupColumn("Files", ImGuiTableColumnFlags.WidthStretch, 0.35f);
        ImGui.TableHeadersRow();

        var metaSpan = MetricsApi.Store.Assets!.Data;
        Span<char> buffer = stackalloc char[16];
        var za = ZaSpanStringBuilder.Create(buffer);
        foreach (var it in metaSpan)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(it.Kind.ToText());

            ImGui.TableSetColumnIndex(1);
            za.Clear();
            RightAlignCellText(za.Append(it.Count).AsSpan());

            ImGui.TableSetColumnIndex(2);
            za.Clear();
            RightAlignCellText(za.Append(it.FileCount).AsSpan());
        }

        ImGui.EndTable();
    }
}