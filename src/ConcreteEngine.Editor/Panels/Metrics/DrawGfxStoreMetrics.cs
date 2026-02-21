using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Graphics.Gfx.Utility;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.UI.GuiLayout;

namespace ConcreteEngine.Editor.Panels.Metrics;

internal static class DrawGfxStoreMetrics
{
    private static int _popupInput = 1;

    public static void Draw(in FrameContext ctx)
    {
        ImGui.SeparatorText("Gfx Metrics"u8);

        if (ImGui.BeginTabBar("metrics_tabs"u8, ImGuiTabBarFlags.FittingPolicyScroll))
        {
            if (ImGui.BeginTabItem("Main"u8))
            {
                DrawMetricsTableClickable(in ctx, false);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Backend"u8))
            {
                DrawMetricsTableClickable(in ctx, true);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }


    private static void DrawMetricsTableClickable(in FrameContext ctx, bool bkStore)
    {
        int cols = bkStore ? 3 : 4;
        if (!ImGui.BeginTable("metrics_table"u8, cols, GuiTheme.TableFlags)) return;

        ImGui.TableSetupColumn("##Name"u8, ImGuiTableColumnFlags.WidthFixed, 26f);
        ImGui.TableSetupColumn("Cnt/Free"u8, ImGuiTableColumnFlags.WidthStretch, 0.8f);
        ImGui.TableSetupColumn("Live/Cap"u8, ImGuiTableColumnFlags.WidthStretch, 0.8f);
        if (!bkStore) ImGui.TableSetupColumn("*"u8, ImGuiTableColumnFlags.WidthStretch, 1f);

        ImGui.TableHeadersRow();
        if (bkStore) DrawBkStore(in ctx);
        else DrawGfxStore(in ctx);
        ImGui.EndTable();
    }

    private static void DrawGfxStore(in FrameContext ctx)
    {
        var descriptions = MetricsApi.Store.GfxMetaDescriptions;
        var metas = MetricsApi.Store.Gfx!.GetData();
        var sw = ctx.Sw;
        sw.Clear();
        for (int i = 0; i < metas.Length; i++)
        {
            scoped ref readonly var it = ref metas[i];
            var desc = descriptions[i];

            ImGui.TableNextRow();
            ImGui.PushID(i);

            ImGui.TableSetColumnIndex(0);
            var open = ImGui.Selectable("##row"u8, false,
                ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);

            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted(ref sw.Write(it.Kind.ToShortText()));

            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted(ref sw.Start(it.Fk.Count).Append("/"u8).Append(it.Fk.Reserved).End());

            ImGui.TableSetColumnIndex(2);
            ImGui.TextUnformatted(ref sw.Start(it.Fk.Active).Append("/"u8).Append(it.Fk.Capacity).End());

            ImGui.SameLine();

            ImGui.TableSetColumnIndex(3);

            NextRightAlignText(ref sw.Write(desc));
            ImGui.TextUnformatted(ref sw.Write(desc));

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

    private static void DrawBkStore(in FrameContext ctx)
    {
        var span = MetricsApi.Store.Gfx!.GetData();
        var sw = ctx.Sw;
        sw.Clear();
        for (int i = 0; i < span.Length; i++)
        {
            scoped ref readonly var it = ref span[i];
            ImGui.TableNextRow();
            ImGui.PushID(i);

            ImGui.TableSetColumnIndex(0);

            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted(ref sw.Write(it.Kind.ToShortText()));

            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted(ref sw.Start(it.Bk.Count).Append("/"u8).Append(it.Bk.Reserved).End());

            ImGui.TableSetColumnIndex(2);
            ImGui.TextUnformatted(ref sw.Start(it.Bk.Active).Append("/"u8).Append(it.Bk.Capacity).End());

            ImGui.PopID();
        }
    }
}