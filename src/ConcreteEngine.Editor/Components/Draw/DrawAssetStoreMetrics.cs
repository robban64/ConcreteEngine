using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components.Draw;

internal static class DrawAssetStoreMetrics
{
    public static void Draw(ref FrameContext ctx)
    {
        ImGui.SeparatorText("Asset Metrics"u8);

        if (!ImGui.BeginTable("asset_store_tbl"u8, 3, GuiTheme.TableFlags)) return;
        ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthStretch, 1.00f);
        ImGui.TableSetupColumn("Count"u8, ImGuiTableColumnFlags.WidthStretch, 0.35f);
        ImGui.TableSetupColumn("Files"u8, ImGuiTableColumnFlags.WidthStretch, 0.35f);
        ImGui.TableHeadersRow();

        var metaSpan = MetricsApi.Store.Assets!.GetData();

        ref var sw = ref ctx.Sw;
        foreach (var it in metaSpan)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(it.Kind.ToTextUtf8());

            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted(sw.Write(it.Count));

            ImGui.TableSetColumnIndex(2);
            ImGui.TextUnformatted(sw.Write(it.FileCount));
        }

        ImGui.EndTable();
    }
}