using System.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.Components.Draw;

internal static class DrawAssetFilePopup
{
    /*
        za.Clear();
        ImGui.TableNextColumn();
        NextAlignText(ColumnWidth, RowHeight);

        if (ImGui.Button(">"u8, new Vector2(ColumnWidth, 22)))
        {
            if (_popupInput < 1) _popupInput = 1;

            TriggerEvent(EventKey.SelectionChanged, it.Id);

            var itemMin = ImGui.GetItemRectMin();
            var itemMax = ImGui.GetItemRectMax();
            ImGui.SetNextWindowPos(new Vector2(itemMax.X + 16, itemMin.Y - 32));
            ImGui.OpenPopup(spanText);
        }

        if (ImGui.IsPopupOpen(spanText))
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f));


        if (ImGui.BeginPopup(spanText))
        {
            DrawAssetFilePopupContent(state, it, ref za);
            ImGui.EndPopup();
            ImGui.PopStyleVar();
        }
*/
    
    public static void DrawAssetFilePopupContent(AssetState state, IAsset asset, ref ZaUtf8SpanWriter za)
    {
        za.Clear();
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(2, 2));
        ImGui.SeparatorText(za.AppendEnd(asset.Name).AsSpan());

        if (ImGui.BeginTable("##asset_detail_object_tbl"u8, 4,
                ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
        {
            var resourceName = AssetUtils.GetAssetResourceIdName(asset, out int resourceValue);
            ImGui.PushID(za.Append(asset.Id).Append(":"u8).AppendEnd(resourceValue).AsSpan());
            ImGui.TableSetupColumn(resourceName, ImGuiTableColumnFlags.WidthFixed, 34);
            ImGui.TableSetupColumn("Gen"u8, ImGuiTableColumnFlags.WidthFixed, 34);
            ImGui.TableSetupColumn("Core"u8, ImGuiTableColumnFlags.WidthFixed);

            ImGui.TableHeadersRow();
            //ImGui.TableSetupColumn(asset.SpecialName, ImGuiTableColumnFlags.WidthFixed);

            ImGui.TableNextRow();

            za.Clear();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(za.AppendEnd(resourceValue).AsSpan());
            za.Clear();

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(za.AppendEnd(asset.Generation).AsSpan());
            za.Clear();

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(StrUtils.BoolToYesNoShort(asset.IsCoreAsset));

            ImGui.PopID();
            za.Clear();

            ImGui.EndTable();
        }

        if (state.FileSpecs.Length > 0)
            DrawFilesTable(state, za);

        /*
        if (asset.HasActions)
        {
            ImGui.Separator();
            if (ImGui.Button("Reload"u8, new Vector2(72, 28)))
            {
                ModelManager.AssetStateContext.TriggerEvent(EventKey.SelectionAction, asset);
                ImGui.CloseCurrentPopup();
            }
        }
        */

        ImGui.PopStyleVar();
        return;

        static void DrawFilesTable(AssetState state, ZaUtf8SpanWriter za)
        {
            GuiUtils.DrawSectionHeader("Files"u8);
            if (!ImGui.BeginTable("##asset_store_files_tbl"u8, 4, ImGuiTableFlags.Borders)) return;
            ImGui.TableSetupColumn("ID"u8, ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Path"u8, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Size"u8, ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Hash"u8, ImGuiTableColumnFlags.WidthFixed);

            ImGui.TableHeadersRow();


            foreach (var it in state.FileSpecs)
            {
                za.Clear();
                ImGui.TableNextRow();
                ImGui.PushID(za.AppendEnd(it.Id.Value).AsSpan());
                za.Clear();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(za.AppendEnd(it.Id.Value).AsSpan());
                za.Clear();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(za.AppendEnd(it.RelativePath).AsSpan());
                za.Clear();

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(za.AppendEnd(it.SizeBytes).AsSpan());
                za.Clear();

                ImGui.TableNextColumn();
                if (it.ContentHash != null)
                    ImGui.Text(za.AppendEnd(it.ContentHash).AsSpan());

                ImGui.PopID();
            }

            za.Clear();

            ImGui.EndTable();
        }
    }
}