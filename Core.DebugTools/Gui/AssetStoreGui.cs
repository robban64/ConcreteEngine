using System.Numerics;
using Core.DebugTools.Data;
using Core.DebugTools.Utils;
using ImGuiNET;
using static Core.DebugTools.Utils.GuiUtils;

namespace Core.DebugTools.Gui;

internal sealed class AssetStoreGui
{
    private readonly AssetStoreViewModel _viewModel;

    private bool _isDirty = true;

    private int _popupInput = 0;

    private const string WindowName = "##EditorAssetStore";

    public AssetStoreGui(AssetStoreViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    private void OnFetchAssetObjectFiles(AssetObjectViewModel asset)
    {
        _viewModel.AssetFileObjects = EditorTable.FetchAssetObjectFiles?.Invoke(asset) ?? [];
    }

    public void Refresh()
    {
        _isDirty = true;
    }

    public void DrawLeft()
    {
        DrawSectionHeader("Asset Store");
        DrawAssetObjects();
        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private void DrawAssetObjects()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit;

        if (!ImGui.BeginTable("##asset_store_object_tbl", 3, flags)) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 24);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Detail", ImGuiTableColumnFlags.WidthFixed, 62);

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(4, 6));
        ImGui.TableHeadersRow();

        foreach (var it in _viewModel.AssetObjects)
        {
            ImGui.PushID(it.AssetId);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            CenterAlignCellText(it.AssetId.ToString());

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(it.Name);

            ImGui.TableNextColumn();
            var popupId = $"row_popup_{it.AssetId}";
            var avail = ImGui.GetContentRegionAvail();
            var btnSize = new Vector2(avail.X, 0);
            if (ImGui.Button("Details", btnSize))
            {
                if (_popupInput < 1) _popupInput = 1;
                OnFetchAssetObjectFiles(it);

                var itemMin = ImGui.GetItemRectMin();
                var itemMax = ImGui.GetItemRectMax();
                ImGui.SetNextWindowPos(new Vector2(itemMax.X + 16, itemMin.Y - 32));
                ImGui.OpenPopup(popupId);
            }

            DrawAssetFilePopup(popupId, it);

            ImGui.PopID();
        }

        ImGui.EndTable();
    }

    private void DrawAssetFilePopup(string popup, AssetObjectViewModel asset)
    {
        if (!ImGui.BeginPopup(popup)) return;
        if (_viewModel.AssetFileObjects.Count == 0) return;

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(4, 6));
        ImGui.SeparatorText(asset.Name);

        if (ImGui.BeginTable("##asset_detail_object_tbl", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("GfxId", ImGuiTableColumnFlags.WidthFixed, 34);
            ImGui.TableSetupColumn("Gen", ImGuiTableColumnFlags.WidthFixed, 34);
            ImGui.TableSetupColumn("IsCore", ImGuiTableColumnFlags.WidthFixed);

            ImGui.TableHeadersRow();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            CenterAlignCellText(asset.GfxId.ToString());

            ImGui.TableNextColumn();
            CenterAlignCellText(asset.Generation.ToString());

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(StringUtils.BoolToYesNo(asset.IsCoreAsset));
            ImGui.EndTable();
        }

        // Header
        DrawSectionHeader("Files");
        if (!ImGui.BeginTable("##asset_store_files_tbl", 5, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
            return;

        // Header
        ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Path", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Gen", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Hash", ImGuiTableColumnFlags.WidthFixed);


        ImGui.TableHeadersRow();

        foreach (var it in _viewModel.AssetFileObjects)
        {
            ImGui.TableNextRow();
            ImGui.PushID(it.AssetFileId);

            ImGui.TableNextColumn();
            CenterAlignCellText(it.AssetFileId.ToString());

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(it.RelativePath);

            ImGui.TableNextColumn();
            CenterAlignCellText(it.SizeInBytes.ToString());

            ImGui.TableNextColumn();
            CenterAlignCellText(it.SizeInBytes.ToString());

            // Hash
            ImGui.TableNextColumn();
            if (it.ContentHash != null)
                ImGui.Text(it.ContentHash);

            ImGui.PopID();
        }

        ImGui.EndTable();

        ImGui.Separator();
        if (ImGui.Button("Reload", new Vector2(72, 28)))
        {
            ImGui.CloseCurrentPopup();
        }

        ImGui.PopStyleVar();
        ImGui.EndPopup();
    }
}