using System.Numerics;
using ImGuiNET;
using Tools.DebugInterface.Data;
using static Tools.DebugInterface.Components.CommonComponents;

namespace Tools.DebugInterface.Components;

internal sealed class DebugLeftPanelGui(DebugDataContainer data)
{
    public void Draw(int width)
    {
        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(vp.WorkPos);

        ImGui.SetNextWindowSize(new Vector2(width, 0f));

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f));
        ImGui.SetNextWindowBgAlpha(0.95f);

        var flags =
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        if (ImGui.Begin("##LeftSidebar", flags))
        {
            DrawSceneMetrics();
            ImGui.Dummy(new Vector2(0, 6));
            DrawAssetStoreTable();
            ImGui.Dummy(new Vector2(0, 6));
            DrawGfxStoreTable();
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private void DrawSceneMetrics()
    {
        DrawSectionHeader("Scene Metrics");
        MetricLine(data.EntityCount);
        MetricLine(data.ShadowMapSize);
    }

    private void DrawAssetStoreTable()
    {
        DrawSectionHeader("Asset Store");

        if (!string.IsNullOrEmpty(data.MaterialDebugInfo))
        {
            ImGui.PushTextWrapPos(0f);
            ImGui.TextDisabled(data.MaterialDebugInfo);
            ImGui.PopTextWrapPos();
            ImGui.Dummy(new Vector2(0, 4));
        }

        if (data.AssetMetrics.Count == 0)
        {
            ImGui.TextDisabled("No asset metas");
            return;
        }

        const ImGuiTableFlags flags =
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit;

        if (ImGui.BeginTable("asset_store_tbl", 3, flags))
        {
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthStretch, 1.00f);
            ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthStretch, 0.35f);
            ImGui.TableSetupColumn("Files", ImGuiTableColumnFlags.WidthStretch, 0.35f);
            ImGui.TableHeadersRow();

            foreach (var (type, pair) in data.AssetMetrics)
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(type);

                ImGui.TableSetColumnIndex(1);
                RightAlignCellText(pair.Item1);

                ImGui.TableSetColumnIndex(2);
                RightAlignCellText(pair.Item2);
            }

            ImGui.EndTable();
        }
    }

    private void DrawGfxStoreTable()
    {
        DrawSectionHeader("GFX Store");

        const ImGuiTableFlags flags =
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.SizingStretchProp;

        if (ImGui.BeginTable("gfx_metrics_table", 3, flags))
        {
            ImGui.TableSetupColumn("Kind", ImGuiTableColumnFlags.WidthStretch, 1.00f);
            ImGui.TableSetupColumn("Gfx", ImGuiTableColumnFlags.WidthStretch, 0.35f);
            ImGui.TableSetupColumn("BK", ImGuiTableColumnFlags.WidthStretch, 0.35f);
            ImGui.TableHeadersRow();

            foreach (var (k, v) in data.GfxStoreMetrics)
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(k);

                ImGui.TableSetColumnIndex(1);
                RightAlignCellText(v.Item1);

                ImGui.TableSetColumnIndex(2);
                RightAlignCellText(v.Item2);
            }

            ImGui.EndTable();
        }
    }
}