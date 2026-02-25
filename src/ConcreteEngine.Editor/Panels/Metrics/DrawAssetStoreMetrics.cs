using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Metrics;

internal static unsafe class DrawAssetStoreMetrics
{
    public static void Draw(FrameContext ctx)
    {
        ImGui.SeparatorText("Asset Metrics"u8);

        if (!ImGui.BeginTable("asset_store_tbl"u8, 3, GuiTheme.TableFlags)) return;
        ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthStretch, 1.00f);
        ImGui.TableSetupColumn("Count"u8, ImGuiTableColumnFlags.WidthStretch, 0.35f);
        ImGui.TableSetupColumn("Files"u8, ImGuiTableColumnFlags.WidthStretch, 0.35f);
        ImGui.TableHeadersRow();

        var metaSpan = MetricsApi.Store.Assets!.GetData();

        foreach (var it in metaSpan)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(ctx.Write(it.Kind.ToText()));

            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted(ctx.Write(it.Count));

            ImGui.TableSetColumnIndex(2);
            ImGui.TextUnformatted(ctx.Write(it.FileCount));
        }

        ImGui.EndTable();
    }
}