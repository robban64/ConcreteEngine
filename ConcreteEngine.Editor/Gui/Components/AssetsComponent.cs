#region

using System.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Editor.ViewModel;
using ImGuiNET;
using static ConcreteEngine.Editor.Utils.GuiUtils;

#endregion

namespace ConcreteEngine.Editor.Gui.Components;

internal static class AssetsComponent
{
    private static readonly string[] AssetTypeArray = ["None", "Shader", "Texture", "Model", "Material"];
    private static AssetStoreViewModel ViewModel => StateCtx.AssetViewModel;

    private static int _popupInput = 0;

    private static void OnAssetSelectedChanged(AssetObjectViewModel? asset)
    {
        EditorService.OnFillAssetFiles(asset);
    }

    private static void OnSelectTypeChange(EditorAssetSelection selection)
    {
        EditorService.OnFillAssetStore(selection);

    }

    public static void DrawSubHeader()
    {
        ImGui.SeparatorText("Asset Store");
        DrawAssetTypeSelector();
        ImGui.Separator();
    }

    public static void Draw()
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

        foreach (var it in ViewModel.AssetObjects)
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

    private static void DrawAssetTypeSelector()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 6));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 6));

        string currentLabel = AssetTypeArray[(int)ViewModel.Selection];
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);
        if (ImGui.BeginCombo("##assetTypeSelector", currentLabel, ImGuiComboFlags.HeightLargest))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 12));

            for (int i = 0; i < AssetTypeArray.Length; i++)
            {
                bool isSelected = i == (int)ViewModel.Selection;

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


    private static void DrawAssetFilePopupContent(AssetObjectViewModel asset)
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

        if (ViewModel.AssetFileObjects.Count > 0)
            DrawFilesTable(formatter, ViewModel);

        if (asset.HasActions)
        {
            ImGui.Separator();
            if (ImGui.Button("Reload", new Vector2(72, 28)))
            {
                StateCtx.ExecuteReloadShader(asset);
                ImGui.CloseCurrentPopup();
            }
        }


        ImGui.PopStyleVar();
        return;

        static void DrawFilesTable(NumberSpanFormatter formatter, AssetStoreViewModel viewModel)
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