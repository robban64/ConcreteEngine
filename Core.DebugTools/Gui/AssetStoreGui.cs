using System.Numerics;
using Core.DebugTools.Data;
using Core.DebugTools.Utils;
using ImGuiNET;
using static Core.DebugTools.Utils.GuiUtils;

namespace Core.DebugTools.Gui;

internal sealed class AssetStoreGui
{
    private int _popupInput = 0;
    private int _currentTypeIndex = 0;

    private static readonly string[] AssetTypeArray = ["Shader", "Texture", "Model", "Material"];
    
    public AssetStoreViewModel ViewModel { get; }

    public AssetStoreGui(AssetStoreViewModel viewModel)
    {
        ViewModel = viewModel;
    }

    private void OnFetchAssetObjectFiles(AssetObjectViewModel asset)
    {
        ViewModel.AssetFileObjects = EditorTable.FetchAssetObjectFiles?.Invoke(asset) ?? [];
    }

    public void DrawLeft()
    {
        ImGui.SeparatorText("Asset Store");
        DrawAssetTypeSelector();
        ImGui.Separator();
        DrawAssetObjects();
        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private void DrawAssetObjects()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerH;
        const int rowHeight = 32;
        
        var btnSize = new Vector2(rowHeight, 22);

        if (!ImGui.BeginTable("##asset_store_object_tbl", 3, flags)) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 32);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, rowHeight);

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(8, 8));

        Span<char> buffer = stackalloc char[10]; 
        var formatter = new NumberSpanFormatter(buffer);

        foreach (var it in ViewModel.AssetObjects)
        {
            ImGui.PushID(it.AssetId);
            ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);

            var bufferStr = formatter.Format(it.AssetId);

            ImGui.TableNextColumn();
            CenterAlignCellTextVertical(bufferStr, rowHeight);

            ImGui.TableNextColumn();
            CenterAlignCellTextVertical(it.Name, rowHeight);

            ImGui.TableNextColumn();

            var itemMinY = ImGui.GetCursorPosY();
            ImGui.SetCursorPosY(itemMinY + (rowHeight - btnSize.Y) * 0.5f);
            if (ImGui.Button(">", btnSize))
            {
                if (_popupInput < 1) _popupInput = 1;
                OnFetchAssetObjectFiles(it);

                var itemMin = ImGui.GetItemRectMin();
                var itemMax = ImGui.GetItemRectMax();
                ImGui.SetNextWindowPos(new Vector2(itemMax.X + 16, itemMin.Y - 32));
                ImGui.OpenPopup(bufferStr);
            }

            DrawAssetFilePopup(bufferStr, it);

            ImGui.PopID();
        }

        ImGui.PopStyleVar();
        ImGui.EndTable();
    }

    private void DrawAssetTypeSelector()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 6));

        string currentLabel = AssetTypeArray[_currentTypeIndex];
        if (ImGui.BeginCombo("##assetTypeSelector", currentLabel, ImGuiComboFlags.HeightLargest))
        {
            for (int i = 0; i < AssetTypeArray.Length; i++)
            {
                bool isSelected = (i == _currentTypeIndex);

                if (ImGui.Selectable(AssetTypeArray[i], isSelected, ImGuiSelectableFlags.None, Vector2.Zero))
                    _currentTypeIndex = i;

                if (i < AssetTypeArray.Length - 1)
                {
                    var p1 = ImGui.GetCursorScreenPos();
                    var width = ImGui.GetContentRegionAvail().X;
                    ImGui.GetWindowDrawList().AddLine(p1, p1 + new Vector2(width, 0),
                        ImGui.GetColorU32(new Vector4(1, 1, 1, 0.05f)));
                }

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }

        ImGui.PopStyleVar(2);
    }


    private void DrawAssetFilePopup(ReadOnlySpan<char> popup, AssetObjectViewModel asset)
    {
        if (!ImGui.BeginPopup(popup)) return;
        if (ViewModel.AssetFileObjects.Count == 0) return;

        Span<char> buffer = stackalloc char[10]; 
        var formatter = new NumberSpanFormatter(buffer);

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
            var bufferStr =  formatter.Format(asset.GfxId);
            CenterAlignCellText(bufferStr);

            ImGui.TableNextColumn();
            bufferStr =  formatter.Format(asset.Generation);
            CenterAlignCellText(bufferStr);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(StringUtils.BoolToYesNo(asset.IsCoreAsset));
            ImGui.EndTable();
        }

        DrawSectionHeader("Files");
        if (!ImGui.BeginTable("##asset_store_files_tbl", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
            return;

        ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Path", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Hash", ImGuiTableColumnFlags.WidthFixed);


        ImGui.TableHeadersRow();

        foreach (var it in ViewModel.AssetFileObjects)
        {
            ImGui.TableNextRow();
            ImGui.PushID(it.AssetFileId);

            var bufferStr =  formatter.Format(it.AssetFileId);
            ImGui.TableNextColumn();
            CenterAlignCellText(bufferStr);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(it.RelativePath);

             bufferStr =  formatter.Format(it.SizeInBytes);
            ImGui.TableNextColumn();
            CenterAlignCellText(bufferStr);

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