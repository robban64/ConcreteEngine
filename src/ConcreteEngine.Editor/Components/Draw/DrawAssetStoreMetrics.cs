using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;
using static ConcreteEngine.Editor.Utils.GuiUtils;

namespace ConcreteEngine.Editor.Components.Draw;

public static class DrawAssetStoreMetrics
{
    public static void Draw(Span<byte> buffer)
    {
        ImGui.SeparatorText("Asset Metrics"u8);

        if (!ImGui.BeginTable("asset_store_tbl"u8, 3, GuiTheme.TableFlags)) return;
        
        ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthStretch, 1.00f);
        ImGui.TableSetupColumn("Count"u8, ImGuiTableColumnFlags.WidthStretch, 0.35f);
        ImGui.TableSetupColumn("Files"u8, ImGuiTableColumnFlags.WidthStretch, 0.35f);
        ImGui.TableHeadersRow();

        var metaSpan = MetricsApi.Store.Assets!.Data;
        var za = ZaUtf8SpanWriter.Create(buffer);
        foreach (var it in metaSpan)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            za.Clear();
            ImGui.TextUnformatted(za.AppendEnd(it.Kind.ToText()).AsSpan());

            ImGui.TableSetColumnIndex(1);
            za.Clear();
            RightAlignCellText(za.Append(it.Count).EndOfBuffer().AsSpan());

            ImGui.TableSetColumnIndex(2);
            za.Clear();
            RightAlignCellText(za.Append(it.FileCount).EndOfBuffer().AsSpan());
        }

        ImGui.EndTable();
    }
}