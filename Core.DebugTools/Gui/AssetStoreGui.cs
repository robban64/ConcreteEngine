using System.Numerics;
using Core.DebugTools.Data;
using Core.DebugTools.Definitions;
using Core.DebugTools.Utils;
using ImGuiNET;
using static Core.DebugTools.Utils.GuiUtils;

namespace Core.DebugTools.Gui;

internal sealed class AssetStoreGui
{
    private int _popupInput = 0;

    private readonly Action<EditorAssetSelection> _selectTypeChanged;
    private readonly Action<AssetObjectViewModel?> _assetSelectedChanged;
    private readonly AssetStoreViewModel _viewModel;

    private static readonly string[] AssetTypeArray = ["None", "Shader", "Texture", "Model", "Material"];

    public AssetStoreGui(EditorStateContext stateContext, Action<EditorAssetSelection> selectTypeChanged,
        Action<AssetObjectViewModel?> assetSelectedChanged)
    {
        _viewModel = stateContext.AssetViewModel;
        _selectTypeChanged = selectTypeChanged;
        _assetSelectedChanged = assetSelectedChanged;
    }

    private void OnSelectTypeChange(EditorAssetSelection selection)
    {
        if (selection == _viewModel.TypeSelection) return;
        _selectTypeChanged(selection);
    }

    public void Draw()
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

        Span<char> buffer = stackalloc char[8];
        var formatter = new NumberSpanFormatter(buffer);

        foreach (var it in _viewModel.AssetObjects)
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
                _assetSelectedChanged(it);

                var itemMin = ImGui.GetItemRectMin();
                var itemMax = ImGui.GetItemRectMax();
                ImGui.SetNextWindowPos(new Vector2(itemMax.X + 16, itemMin.Y - 32));
                ImGui.OpenPopup(bufferStr);
            }

            if (ImGui.BeginPopup(bufferStr))
            {
                DrawAssetFilePopupContent(it);
                ImGui.EndPopup();
            }

            ImGui.PopID();
        }

        ImGui.PopStyleVar();
        ImGui.EndTable();
    }

    private void DrawAssetTypeSelector()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 6));

        string currentLabel = AssetTypeArray[(int)_viewModel.TypeSelection];
        if (ImGui.BeginCombo("##assetTypeSelector", currentLabel, ImGuiComboFlags.HeightLargest))
        {
            for (int i = 0; i < AssetTypeArray.Length; i++)
            {
                bool isSelected = (i == (int)_viewModel.TypeSelection);

                if (ImGui.Selectable(AssetTypeArray[i], isSelected, ImGuiSelectableFlags.None, Vector2.Zero))
                    OnSelectTypeChange((EditorAssetSelection)i);

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


    private void DrawAssetFilePopupContent(AssetObjectViewModel asset)
    {
        if (_viewModel.AssetFileObjects.Count == 0) return;

        Span<char> buffer = stackalloc char[8];
        var formatter = new NumberSpanFormatter(buffer);

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(4, 6));
        ImGui.SeparatorText(asset.Name);

        if (ImGui.BeginTable("##asset_detail_object_tbl", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn(asset.ResourceName, ImGuiTableColumnFlags.WidthFixed, 34);
            ImGui.TableSetupColumn("Gen", ImGuiTableColumnFlags.WidthFixed, 34);
            ImGui.TableSetupColumn("Core", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn(asset.SpecialName, ImGuiTableColumnFlags.WidthFixed);

            ImGui.TableHeadersRow();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var bufferStr = formatter.Format(asset.ResourceId);
            CenterAlignCellText(bufferStr);

            ImGui.TableNextColumn();
            bufferStr = formatter.Format(asset.Generation);
            CenterAlignCellText(bufferStr);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(StringUtils.BoolToYesNo(asset.IsCoreAsset));

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(asset.SpecialValue);

            ImGui.EndTable();
        }

        DrawSectionHeader("Files");
        if (ImGui.BeginTable("##asset_store_files_tbl", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Path", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Hash", ImGuiTableColumnFlags.WidthFixed);


            ImGui.TableHeadersRow();

            foreach (var it in _viewModel.AssetFileObjects)
            {
                ImGui.TableNextRow();
                ImGui.PushID(it.AssetFileId);

                var bufferStr = formatter.Format(it.AssetFileId);
                ImGui.TableNextColumn();
                CenterAlignCellText(bufferStr);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(it.RelativePath);

                bufferStr = formatter.Format(it.SizeInBytes);
                ImGui.TableNextColumn();
                CenterAlignCellText(bufferStr);

                ImGui.TableNextColumn();
                if (it.ContentHash != null)
                    ImGui.Text(it.ContentHash);

                ImGui.PopID();
            }

            ImGui.EndTable();
        }

        if (asset.HasActions)
        {
            ImGui.Separator();
            if (ImGui.Button("Reload", new Vector2(72, 28)))
            {
                ImGui.CloseCurrentPopup();
            }
        }


        ImGui.PopStyleVar();
    }
}