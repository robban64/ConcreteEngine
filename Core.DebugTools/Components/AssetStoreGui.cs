using System.Numerics;
using Core.DebugTools.Data;
using ImGuiNET;
using static Core.DebugTools.Components.CommonComponents;

namespace Core.DebugTools.Components;

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
        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(vp.WorkPos);

        ImGui.SetNextWindowSize(new Vector2(300f, 0f));

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f));
        ImGui.SetNextWindowBgAlpha(0.95f);

        const ImGuiWindowFlags windowFlags =
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        if (ImGui.Begin("##EditorAssetStore", windowFlags))
        {
            DrawSectionHeader("Asset Store");
        }

        DrawAssetObjects();
        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private void DrawAssetObjects()
    {
        const ImGuiTableFlags flags =
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit;

        if (!ImGui.BeginTable("##asset_store_object_tbl", 6, flags)) return;
        
        ImGui.TableSetupColumn("AId", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("GId", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Core", ImGuiTableColumnFlags.WidthFixed, 24);
        ImGui.TableSetupColumn("Gen", ImGuiTableColumnFlags.WidthFixed, 24);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed);

        ImGui.TableHeadersRow();

        foreach (var it in _viewModel.AssetObjects)
        {
            ImGui.TableNextRow();
            ImGui.PushID(it.AssetId);

            ImGui.TableSetColumnIndex(0);
            CenterAlignCellText(it.AssetId.ToString());
            ImGui.TableSetColumnIndex(1);
            CenterAlignCellText(it.GfxId.ToString());

            ImGui.TableSetColumnIndex(2);
            ImGui.TextUnformatted(it.Name);

            ImGui.TableSetColumnIndex(3);
            CenterAlignCellText(StringUtils.BoolToYesNo(it.IsCoreAsset));

            ImGui.TableSetColumnIndex(4);
            CenterAlignCellText(it.Generation.ToString());

            ImGui.TableSetColumnIndex(5);
            bool open = ImGui.SmallButton("*");
            if (open)
            {
                if (_popupInput < 1) _popupInput = 1;
                OnFetchAssetObjectFiles(it);
                ImGui.OpenPopup("row_popup");
            }

            DrawAssetFilePopup("row_popup", it);
            
            ImGui.PopID();
        }

        ImGui.EndTable();
    }

    private void DrawAssetFilePopup(string popup, AssetObjectViewModel asset)
    {
        if (!ImGui.BeginPopup(popup)) return;
        if (_viewModel.AssetFileObjects.Count == 0) return;
        
        DrawSectionHeader(asset.Name);
        
        if (!ImGui.BeginTable("##asset_store_files_tbl", 5, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.Borders))
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

        if (ImGui.Button("Reload"))
        {
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }
}