#region

using System.Numerics;
using Core.DebugTools.Data;
using ImGuiNET;
using static Core.DebugTools.Components.CommonComponents;

#endregion

namespace Core.DebugTools.Components;

internal sealed class DebugLeftPanelGui(DebugTextData data)
{
    private static int _popupInput = 1;

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
        MetricLine(data.SceneMetrics.EntityCount);
        MetricLine(data.SceneMetrics.ShadowMapSize);
    }

    private void DrawAssetStoreTable()
    {
        DrawSectionHeader("Asset Store");

        if (!string.IsNullOrEmpty(data.MaterialMetrics))
        {
            ImGui.PushTextWrapPos(0f);
            ImGui.TextDisabled(data.MaterialMetrics);
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

            foreach (var it in data.AssetMetrics)
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(it.Name);

                ImGui.TableSetColumnIndex(1);
                RightAlignCellText(it.Count);

                ImGui.TableSetColumnIndex(2);
                RightAlignCellText(it.Files);
            }

            ImGui.EndTable();
        }
    }

    private void DrawGfxStoreTable()
    {
        DrawSectionHeader("GFX Store");
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));
        if (ImGui.BeginTabBar("metrics_tabs", ImGuiTabBarFlags.FittingPolicyScroll))
        {
            if (ImGui.BeginTabItem("Main"))
            {
                DrawMetricsTableClickable(data.GfxStoreMetrics, false);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Back"))
            {
                DrawMetricsTableClickable(data.GfxStoreMetrics, true);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.PopStyleVar();
    }


    private static void DrawMetricsTableClickable(List<GfxStoreTextRecord> metrics, bool bkStore)
    {
        const ImGuiTableFlags flags =
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.SizingStretchProp;

        int cols = bkStore ? 3 : 4;

        if (!ImGui.BeginTable("metrics_table", cols, flags)) return;

        ImGui.TableSetupColumn("Nam", ImGuiTableColumnFlags.WidthFixed, 30f);
        ImGui.TableSetupColumn("Cnt/Free", ImGuiTableColumnFlags.WidthStretch, 0.8f);
        ImGui.TableSetupColumn("Live/Cap", ImGuiTableColumnFlags.WidthStretch, 0.8f);
        if(!bkStore)         ImGui.TableSetupColumn("*", ImGuiTableColumnFlags.WidthStretch, 1f);

        ImGui.TableHeadersRow();

        for (int i = 0; i < metrics.Count; i++)
        {
            var it = metrics[i];
            ImGui.TableNextRow();
            ImGui.PushID(i);

            ImGui.TableSetColumnIndex(0);
            bool open = ImGui.Selectable("##row",
                false,
                ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);

            var count = bkStore ? it.BkStore.StoreCount : it.GfxStore.StoreCount;
            var alive = bkStore ? it.BkStore.StoreAliveCap : it.GfxStore.StoreAliveCap;
            var special = bkStore ? it.BkStore.SpecialMetric : it.GfxStore.SpecialMetric;

            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted(it.SimpleName);

            ImGui.TableSetColumnIndex(1);
            RightAlignCellText(count);

            ImGui.TableSetColumnIndex(2);
            RightAlignCellText(alive);
            ImGui.SameLine();
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, 2));
            ImGui.PopStyleVar();

            if (!bkStore)
            {
                ImGui.TableSetColumnIndex(3);
                RightAlignCellText(special);
            }

            if (open)
            {
                if (_popupInput < 1) _popupInput = 1;
                ImGui.OpenPopup("row_popup");
            }

            if (ImGui.BeginPopup("row_popup"))
            {
                ImGui.TextUnformatted("Id");
                ImGui.SameLine();
                ImGui.InputInt("##Id", ref _popupInput);
                if (_popupInput < 1) _popupInput = 1;

                bool canPrint = _popupInput >= 1;
                if (!canPrint) ImGui.BeginDisabled();
                if (ImGui.Button("Print"))
                {
                    ImGui.CloseCurrentPopup();
                }

                if (!canPrint) ImGui.EndDisabled();

                ImGui.EndPopup();
            }

            ImGui.PopID();
        }

        ImGui.EndTable();
    }

    private static void DrawGfxStoreTable(List<GfxStoreTextRecord> metrics, bool bkStore)
    {
        const ImGuiTableFlags flags =
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.SizingStretchProp;

        if (!ImGui.BeginTable("metrics_table", 3, flags))
            return;

        ImGui.TableSetupColumn("Kind", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Cnt/Free", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Live/Cap", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        foreach (var it in metrics)
        {
            var count = bkStore ? it.BkStore.StoreCount : it.GfxStore.StoreCount;
            var alive = bkStore ? it.BkStore.StoreAliveCap : it.GfxStore.StoreAliveCap;
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(it.SimpleName);

            ImGui.TableSetColumnIndex(1);
            RightAlignCellText(count);

            ImGui.TableSetColumnIndex(2);
            RightAlignCellText(alive);
        }

        ImGui.EndTable();
    }
}