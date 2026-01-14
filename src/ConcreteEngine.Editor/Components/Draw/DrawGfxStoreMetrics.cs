using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Utility;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;
using static ConcreteEngine.Editor.UI.GuiUtils;

namespace ConcreteEngine.Editor.Components.Draw;

internal static class DrawGfxStoreMetrics
{
    private static int _popupInput = 1;

    public static void Draw(Span<byte> buffer)
    {
        ImGui.SeparatorText("Gfx Metrics"u8);

        if (ImGui.BeginTabBar("metrics_tabs"u8, ImGuiTabBarFlags.FittingPolicyScroll))
        {
            if (ImGui.BeginTabItem("Main"u8))
            {
                DrawMetricsTableClickable(buffer, false);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Backend"u8))
            {
                DrawMetricsTableClickable(buffer, true);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }


    private static void DrawMetricsTableClickable(Span<byte> buffer, bool bkStore)
    {
        int cols = bkStore ? 3 : 4;
        if (!ImGui.BeginTable("metrics_table"u8, cols, GuiTheme.TableFlags)) return;

        ImGui.TableSetupColumn("##Name"u8, ImGuiTableColumnFlags.WidthFixed, 26f);
        ImGui.TableSetupColumn("Cnt/Free"u8, ImGuiTableColumnFlags.WidthStretch, 0.8f);
        ImGui.TableSetupColumn("Live/Cap"u8, ImGuiTableColumnFlags.WidthStretch, 0.8f);
        if (!bkStore) ImGui.TableSetupColumn("*"u8, ImGuiTableColumnFlags.WidthStretch, 1f);

        ImGui.TableHeadersRow();
        if (bkStore) DrawBkStore(buffer);
        else DrawGfxStore(buffer);
        ImGui.EndTable();
    }

    private static void DrawGfxStore(Span<byte> buffer)
    {
        var descriptions = MetricsApi.Store.GfxMetaDescriptions;
        var metas = MetricsApi.Store.Gfx!.GetData();

        var za = ZaUtf8SpanWriter.Create(buffer);
        for (int i = 0; i < metas.Length; i++)
        {
            ref readonly var it = ref metas[i];
            var desc = descriptions[i];
            za.Clear();

            ImGui.TableNextRow();
            ImGui.PushID(i);

            ImGui.TableSetColumnIndex(0);
            var open = ImGui.Selectable("##row"u8, false,
                ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);

            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted(za.AppendEnd(it.Kind.ToShortText()).AsSpan());

            ImGui.TableSetColumnIndex(1);
            za.Clear();
            ImGui.TextUnformatted(za.Append(it.Fk.Count).Append("/"u8).AppendEnd(it.Fk.Reserved).AsSpan());

            ImGui.TableSetColumnIndex(2);
            za.Clear();
            ImGui.TextUnformatted(za.Append(it.Fk.Active).Append("/"u8).AppendEnd(it.Fk.Capacity).AsSpan());

            ImGui.SameLine();

            za.Clear();
            ImGui.TableSetColumnIndex(3);
            RightAlignCellText(za.AppendEnd(desc).AsSpan());

            if (open)
            {
                if (_popupInput < 1) _popupInput = 1;
                ImGui.OpenPopup("row_popup"u8);
            }

            if (ImGui.BeginPopup("row_popup"u8))
            {
                ImGui.TextUnformatted("Id"u8);
                ImGui.SameLine();
                ImGui.InputInt("##Idu8", ref _popupInput);
                if (_popupInput < 1) _popupInput = 1;

                var canPrint = _popupInput >= 1;
                if (!canPrint) ImGui.BeginDisabled();
                if (ImGui.Button("Print"u8))
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
        var span = MetricsApi.Store.Gfx!.GetData();
        var za = ZaUtf8SpanWriter.Create(buffer);
        for (int i = 0; i < span.Length; i++)
        {
            ref readonly var it = ref span[i];
            ImGui.TableNextRow();
            ImGui.PushID(i);

            ImGui.TableSetColumnIndex(0);

            za.Clear();
            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted(za.AppendEnd(it.Kind.ToShortText()).AsSpan());
            za.Clear();

            ImGui.TableSetColumnIndex(1);
            za.Clear();
            ImGui.TextUnformatted(za.Append(it.Bk.Count).Append("/"u8).AppendEnd(it.Bk.Reserved).AsSpan());

            ImGui.TableSetColumnIndex(2);
            za.Clear();
            ImGui.TextUnformatted(za.Append(it.Bk.Active).Append("/"u8).AppendEnd(it.Bk.Capacity).AsSpan());


            ImGui.PopID();
        }
    }
}