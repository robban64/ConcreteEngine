#region

using System.Numerics;
using ImGuiNET;
using static ConcreteEngine.Editor.Utils.GuiUtils;

#endregion

namespace ConcreteEngine.Editor.Metrics;

internal static class GfxStoreMetricsGui
{
    private static int _popupInput = 1;

    public static void DrawGfxStoreMetrics(MetricReport data)
    {
        ImGui.SeparatorText("Gfx Metrics");
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

        ImGui.PopStyleVar(1);
    }


    private static void DrawMetricsTableClickable(List<GfxStoreMetricTextRecord> metrics, bool bkStore)
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
        if (!bkStore) ImGui.TableSetupColumn("*", ImGuiTableColumnFlags.WidthStretch, 1f);

        ImGui.TableHeadersRow();

        for (int i = 0; i < metrics.Count; i++)
        {
            var it = metrics[i];
            ImGui.TableNextRow();
            ImGui.PushID(i);

            ImGui.TableSetColumnIndex(0);
            var open = ImGui.Selectable("##row",
                false,
                ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);

            var count = bkStore ? it.BkStore.StoreCount : it.GfxStore.StoreCount;
            var alive = bkStore ? it.BkStore.StoreAliveCap : it.GfxStore.StoreAliveCap;
            var special = bkStore ? it.BkStore.SpecialMetric : it.GfxStore.SpecialMetric;

            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted(it.ShortName);

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

                var canPrint = _popupInput >= 1;
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
}