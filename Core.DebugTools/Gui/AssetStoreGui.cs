using System.Numerics;
using Core.DebugTools.Data;
using Core.DebugTools.Definitions;
using Core.DebugTools.Editor;
using Core.DebugTools.Utils;
using ImGuiNET;
using static Core.DebugTools.Utils.GuiUtils;

namespace Core.DebugTools.Gui;

internal sealed class AssetStoreGui
{
    private int _popupInput = 0;

    private readonly EditorStateContext _ctx;
    private readonly AssetStoreViewModel _viewModel;

    private static readonly string[] AssetTypeArray = ["None", "Shader", "Texture", "Model", "Material"];

    public AssetStoreGui(EditorStateContext ctx)
    {
        _ctx  = ctx;
        _viewModel = ctx.AssetViewModel;
    }
    
    private void OnAssetSelectedChanged(AssetObjectViewModel? asset)
    {
        _viewModel.AssetFileObjects.Clear();
        if (asset is null) return;
        EditorTable.FetchAssetObjectFiles?.Invoke(asset, _viewModel.AssetFileObjects);
    }

    private void OnSelectTypeChange(EditorAssetSelection selection)
    {
        if (selection == _viewModel.TypeSelection) return;
        _viewModel.TypeSelection = selection;
        _viewModel.AssetObjects.Clear();
        _viewModel.AssetFileObjects.Clear();

        if (selection == EditorAssetSelection.None) return;

        EditorTable.FillAssetStoreView?.Invoke(selection, _viewModel.AssetObjects);
    }

    public void DrawSubHeader()
    {
        ImGui.SeparatorText("Asset Store");
        DrawAssetTypeSelector();
        ImGui.Separator();

    }

    public void Draw()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerH;
        const int rowHeight = 32;

        var btnSize = new Vector2(rowHeight, 22);

        if (!ImGui.BeginTable("##asset_store_object_tbl", 3, flags)) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 32);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, rowHeight);

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(8, 8));

        //Span<char> buffer = stackalloc char[8];
        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer16);

        foreach (var it in _viewModel.AssetObjects)
        {
            ImGui.PushID(it.AssetId);
            ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);

            var bufferStr = formatter.Format(it.AssetId);

            ImGui.TableNextColumn();
            CenterAlignTextVertical(bufferStr, rowHeight);

            ImGui.TableNextColumn();
            CenterAlignTextVertical(it.Name, rowHeight);

            ImGui.TableNextColumn();

            var itemMinY = ImGui.GetCursorPosY();
            ImGui.SetCursorPosY(itemMinY + (rowHeight - btnSize.Y) * 0.5f);
            if (ImGui.Button(">", btnSize))
            {
                if (_popupInput < 1) _popupInput = 1;
                OnAssetSelectedChanged(it);

                var itemMin = ImGui.GetItemRectMin();
                var itemMax = ImGui.GetItemRectMax();
                ImGui.SetNextWindowPos(new Vector2(itemMax.X + 16, itemMin.Y - 32));
                ImGui.OpenPopup(bufferStr);
            }

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f));
            if (ImGui.BeginPopup(bufferStr))
            {
                DrawAssetFilePopupContent(it);
                ImGui.EndPopup();
            }
            ImGui.PopStyleVar();

            ImGui.PopID();
        }

        ImGui.PopStyleVar();
        ImGui.EndTable();
    }

    private void DrawAssetTypeSelector()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 6));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 6));

        string currentLabel = AssetTypeArray[(int)_viewModel.TypeSelection];
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);
        if (ImGui.BeginCombo("##assetTypeSelector", currentLabel, ImGuiComboFlags.HeightLargest))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 12));

            for (int i = 0; i < AssetTypeArray.Length; i++)
            {
                bool isSelected = (i == (int)_viewModel.TypeSelection);

                if (ImGui.Selectable(AssetTypeArray[i], isSelected, ImGuiSelectableFlags.None, Vector2.Zero))
                    OnSelectTypeChange((EditorAssetSelection)i);

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.PopStyleVar();

            ImGui.EndCombo();
        }

        ImGui.PopStyleVar(2);
    }


    private void DrawAssetFilePopupContent(AssetObjectViewModel asset)
    {
        //Span<char> buffer = stackalloc char[8];
        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer8);

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
            CenterAlignTextHorizontal(bufferStr);

            ImGui.TableNextColumn();
            bufferStr = formatter.Format(asset.Generation);
            CenterAlignTextHorizontal(bufferStr);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(StringUtils.BoolToYesNo(asset.IsCoreAsset));

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(asset.SpecialValue);

            ImGui.EndTable();
        }

        if (_viewModel.AssetFileObjects.Count > 0)
            DrawFilesTable(formatter, _viewModel);

        if (asset.HasActions)
        {
            ImGui.Separator();
            if (ImGui.Button("Reload", new Vector2(72, 28)))
            {
                _ctx.ExecuteReloadShader(asset);
                ImGui.CloseCurrentPopup();
            }
        }


        ImGui.PopStyleVar();
        return;

        static void DrawFilesTable(NumberSpanFormatter formatter,AssetStoreViewModel viewModel)
        {
            DrawSectionHeader("Files");
            if (!ImGui.BeginTable("##asset_store_files_tbl", 4,
                    ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders)) return;
            ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Path", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Hash", ImGuiTableColumnFlags.WidthFixed);

            ImGui.TableHeadersRow();

            foreach (var it in viewModel.AssetFileObjects)
            {
                ImGui.TableNextRow();
                ImGui.PushID(it.AssetFileId);

                var bufferStr = formatter.Format(it.AssetFileId);
                ImGui.TableNextColumn();
                CenterAlignTextHorizontal(bufferStr);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(it.RelativePath);

                bufferStr = formatter.Format(it.SizeInBytes);
                ImGui.TableNextColumn();
                CenterAlignTextHorizontal(bufferStr);

                ImGui.TableNextColumn();
                if (it.ContentHash != null)
                    ImGui.Text(it.ContentHash);

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }
}