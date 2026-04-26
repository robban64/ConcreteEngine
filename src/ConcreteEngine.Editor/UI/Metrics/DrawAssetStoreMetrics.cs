using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Engine.Assets.Extensions;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Metrics;

internal static unsafe class DrawAssetStoreMetrics
{
    public static void Draw(AssetsMetaInfo[] assetStore)
    {
        ImGui.SeparatorText("Asset Metrics"u8);

        if (!ImGui.BeginTable("asset_store_tbl"u8, 3, GuiTheme.TableFlags)) return;
        ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthStretch, 1.00f);
        ImGui.TableSetupColumn("Count"u8, ImGuiTableColumnFlags.WidthStretch, 0.35f);
        ImGui.TableSetupColumn("Files"u8, ImGuiTableColumnFlags.WidthStretch, 0.35f);
        ImGui.TableHeadersRow();

        var sw = TextBuffers.GetWriter();
        foreach (var it in assetStore)
        {
            ImGui.TableNextRow();
            
            ImGui.NextColumn();
            ImGui.TextUnformatted(sw.Write(it.Kind.ToText()));

            ImGui.NextColumn();
            ImGui.TextUnformatted(sw.Write(it.Count));

            ImGui.NextColumn();
            ImGui.TextUnformatted(sw.Write(it.FileCount));
        }

        ImGui.EndTable();
    }
}