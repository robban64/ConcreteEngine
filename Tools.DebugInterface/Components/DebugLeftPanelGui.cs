using System.Numerics;
using ImGuiNET;

namespace Tools.DebugInterface.Components;

internal sealed class DebugLeftPanelGui(DebugDataContainer data)
{
    public void Draw(int width)
    {
        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(vp.WorkPos);
        ImGui.SetNextWindowSize(new Vector2(width, 0f));

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 8f));
        ImGui.Begin("##LeftSidebar",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus);

        DrawSceneMetrics();
        DrawAssetStoreTable();
        DrawGfxStoreTable();

        ImGui.End();
        ImGui.PopStyleVar();
    }

    private void DrawSceneMetrics()
    {
        ImGui.TextUnformatted("Scene Metrics");
        ImGui.Separator();
        ImGui.TextUnformatted(data.EntityCount);
        ImGui.TextUnformatted(data.ShadowMapSize);
        ImGui.Separator();
    }

    private void DrawAssetStoreTable()
    {
        ImGui.TextUnformatted("Asset Store");
        ImGui.Separator();
        ImGui.TextUnformatted(data.MaterialDebugInfo);
        ImGui.Separator();


        if (data.AssetMetrics.Count == 0)
        {
            ImGui.TextDisabled("No asset metas");
            return;
        }

        if (ImGui.BeginTable("asset_store_tbl", 3,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Files", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();

            foreach (var (type, (count, fileCount)) in data.AssetMetrics)
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(type);

                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted(count);

                ImGui.TableSetColumnIndex(2);
                ImGui.TextUnformatted(fileCount);
            }

            ImGui.EndTable();
        }

        ImGui.Separator();
    }


    private void DrawGfxStoreTable()
    {
        ImGui.TextUnformatted("GFX Store");
        ImGui.Separator();
        if (ImGui.BeginTable("gfx_metrics_table", 3,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Kind", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Gfx", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("BK", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();

            var dict = data.GfxStoreMetrics;
            foreach (var (k, v) in dict)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(k);

                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted(v.Item1);

                ImGui.TableSetColumnIndex(2);
                ImGui.TextUnformatted(v.Item2);
            }

            ImGui.EndTable();
        }

        ImGui.Separator();
    }
}