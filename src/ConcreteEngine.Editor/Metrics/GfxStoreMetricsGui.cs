using System.Numerics;
using ConcreteEngine.Core.Specs.Graphics;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;
using static ConcreteEngine.Editor.Utils.GuiUtils;

namespace ConcreteEngine.Editor.Metrics;

internal static class GfxStoreMetricsGui
{
    private static int _popupInput = 1;

    public static void DrawGfxStoreMetrics()
    {
        ImGui.SeparatorText("Gfx Metrics");
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));

        if (ImGui.BeginTabBar("metrics_tabs", ImGuiTabBarFlags.FittingPolicyScroll))
        {
            Span<byte> buffer = stackalloc byte[32];

            if (ImGui.BeginTabItem("Main"))
            {
                DrawMetricsTableClickable(buffer, false);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Back"))
            {
                DrawMetricsTableClickable(buffer, true);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.PopStyleVar(1);
    }


    private static void DrawMetricsTableClickable(Span<byte> buffer, bool bkStore)
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
        if (bkStore) DrawBkStore(buffer);
        else DrawGfxStore(buffer);
        ImGui.EndTable();
    }

    private static void DrawGfxStore(Span<byte> buffer)
    {
        var descriptions = MetricsApi.Store.GfxMetaDescriptions;
        var metas = MetricsApi.Store.Gfx!.Data;

        var za = ZaUtf8SpanWriter.Create(buffer);
        for (int i = 0; i < metas.Length; i++)
        {
            ref readonly var it = ref metas[i];
            var desc = descriptions[i];

            ImGui.TableNextRow();
            ImGui.PushID(i);

            ImGui.TableSetColumnIndex(0);
            var open = ImGui.Selectable("##row", false,
                ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);

            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted(it.Kind.ToShortText());

            ImGui.TableSetColumnIndex(1);
            za.Clear();
            RightAlignCellText(za.Append(it.Fk.Count).Append("/").Append(it.Fk.Reserved).AsSpan());

            ImGui.TableSetColumnIndex(2);
            za.Clear();
            RightAlignCellText(za.Append(it.Fk.Active).Append("/").Append(it.Fk.Capacity).AsSpan());

            ImGui.SameLine();
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, 2));
            ImGui.PopStyleVar();

            za.Clear();
            ImGui.TableSetColumnIndex(3);
            RightAlignCellText(za.Append(desc).AsSpan());

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
    }

    private static void DrawBkStore(Span<byte> buffer)
    {
        var span = MetricsApi.Store.Gfx!.Data;
        var za = ZaUtf8SpanWriter.Create(buffer);
        for (int i = 0; i < span.Length; i++)
        {
            ref readonly var it = ref span[i];
            ImGui.TableNextRow();
            ImGui.PushID(i);

            ImGui.TableSetColumnIndex(0);

            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted(it.Kind.ToShortText());

            ImGui.TableSetColumnIndex(1);
            za.Clear();
            RightAlignCellText(za.Append(it.Bk.Count).Append("/").Append(it.Bk.Reserved).AsSpan());

            ImGui.TableSetColumnIndex(2);
            za.Clear();
            RightAlignCellText(za.Append(it.Bk.Active).Append("/").Append(it.Bk.Capacity).AsSpan());


            ImGui.PopID();
        }
    }
}