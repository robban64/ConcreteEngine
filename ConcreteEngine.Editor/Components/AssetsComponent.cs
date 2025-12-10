#region

using System.Numerics;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;
using static ConcreteEngine.Editor.Utils.GuiUtils;

#endregion

namespace ConcreteEngine.Editor.Components;

internal static class AssetsComponent
{
    private const int RowHeight = 32;
    private static readonly Vector2 BtnSize = new(RowHeight, 22);

    private static readonly string[] AssetCategoryNames = ["None", "Shader", "Texture", "Model", "Material"];

    private static int _popupInput = 0;

    private static ModelStateContext<AssetState> Model => ModelManager.AssetStateContext;

    private static AssetState State => Model.State!;

    private static void OnCategoryChanged(EditorAssetCategory category)
    {
        if (category == State.Category) return;
        State.SetCategory(category);
        Model.TriggerEvent(EventKey.CategoryChanged);
    }

    private static void OnSelectionChanged(EditorAssetResource? asset) =>
        Model.TriggerEvent(EventKey.SelectionChanged, asset);

    public static unsafe void Draw()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoBordersInBody |
                                      ImGuiTableFlags.ScrollY;

        ImGui.SeparatorText("Asset Store");
        DrawAssetTypeSelector();
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(12, 0));
        if (!ImGui.BeginTable("##asset_store_object_tbl", 3, flags)) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 36);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##asset-tbl-separator-col", ImGuiTableColumnFlags.WidthFixed, RowHeight);

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

        ImGui.TableNextColumn();
        CenterAlignTextHorizontal("Id");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Name");

        ImGui.TableNextColumn();
        CenterAlignTextHorizontal("");

        ImGui.TableNextColumn();

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0, 8));


        //Span<char> buffer = stackalloc char[8];
        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer16);

        var assetSpan = State.GetAssetSpan();

        var rowHeight = ImGui.GetFrameHeight() + 8;
        var clipper = new ImGuiListClipper();
        ImGuiNative.ImGuiListClipper_Begin(&clipper, assetSpan.Length, rowHeight);
        while (ImGuiNative.ImGuiListClipper_Step(&clipper) != 0)
        {
            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                DrawListItem(i, assetSpan, formatter);
        }

        ImGuiNative.ImGuiListClipper_End(&clipper);


        ImGui.PopStyleVar(2);
        ImGui.EndTable();
    }

    private static void DrawListItem(int i, ReadOnlySpan<EditorAssetResource> assets, NumberSpanFormatter formatter)
    {
        var it = assets[i];
        ImGui.PushID(it.Id.Identifier);
        ImGui.TableNextRow(ImGuiTableRowFlags.None, RowHeight);

        var bufferStr = formatter.Format(it.Id.Identifier);

        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(bufferStr);

        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(it.Name);

        ImGui.TableNextColumn();

        if (ImGui.Button(">", BtnSize))
        {
            if (_popupInput < 1) _popupInput = 1;
            OnSelectionChanged(it);

            var itemMin = ImGui.GetItemRectMin();
            var itemMax = ImGui.GetItemRectMax();
            ImGui.SetNextWindowPos(new Vector2(itemMax.X + 16, itemMin.Y - 32));
            ImGui.OpenPopup(bufferStr);
        }

        if (ImGui.IsPopupOpen(bufferStr))
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f));


        if (ImGui.BeginPopup(bufferStr))
        {
            DrawAssetFilePopupContent(it);
            ImGui.EndPopup();
            ImGui.PopStyleVar();
        }


        ImGui.PopID();
    }

    private static void DrawAssetTypeSelector()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 6));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 6));

        var currentLabel = AssetCategoryNames[(int)State.Category];
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);
        if (ImGui.BeginCombo("##assetTypeSelector", currentLabel, ImGuiComboFlags.HeightLargest))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 12));

            for (var i = 0; i < AssetCategoryNames.Length; i++)
            {
                var isSelected = i == (int)State.Category;

                if (ImGui.Selectable(AssetCategoryNames[i], isSelected, ImGuiSelectableFlags.None, Vector2.Zero))
                    OnCategoryChanged((EditorAssetCategory)i);

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.PopStyleVar();

            ImGui.EndCombo();
        }

        ImGui.PopStyleVar(2);
    }


    private static void DrawAssetFilePopupContent(EditorAssetResource asset)
    {
        //Span<char> buffer = stackalloc char[8];
        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer8);

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(2, 2));
        ImGui.SeparatorText(asset.Name);

        if (ImGui.BeginTable("##asset_detail_object_tbl", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
        {
            ImGui.PushID(asset.Id);
            ImGui.TableSetupColumn(asset.ResourceName, ImGuiTableColumnFlags.WidthFixed, 34);
            ImGui.TableSetupColumn("Gen", ImGuiTableColumnFlags.WidthFixed, 34);
            ImGui.TableSetupColumn("Core", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn(asset.SpecialName, ImGuiTableColumnFlags.WidthFixed);

            ImGui.TableHeadersRow();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var bufferStr = formatter.Format(asset.ResourceId);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(bufferStr);

            ImGui.TableNextColumn();
            bufferStr = formatter.Format(asset.Generation);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(bufferStr);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(StringUtils.BoolToYesNo(asset.IsCoreAsset));

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(asset.SpecialValue);
            ImGui.PopID();

            ImGui.EndTable();
        }

        if (State.FileAssets.Length > 0)
            DrawFilesTable(formatter, State);

        if (asset.HasActions)
        {
            ImGui.Separator();
            if (ImGui.Button("Reload", new Vector2(72, 28)))
            {
                ModelManager.AssetStateContext.TriggerEvent(EventKey.SelectionAction, asset);
                ImGui.CloseCurrentPopup();
            }
        }


        ImGui.PopStyleVar();
        return;

        static void DrawFilesTable(NumberSpanFormatter formatter, AssetState viewModel)
        {
            DrawSectionHeader("Files");
            if (!ImGui.BeginTable("##asset_store_files_tbl", 4, ImGuiTableFlags.Borders)) return;
            ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Path", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Hash", ImGuiTableColumnFlags.WidthFixed);

            ImGui.TableHeadersRow();

            foreach (var it in viewModel.FileAssets)
            {
                ImGui.TableNextRow();
                ImGui.PushID(it.AssetFileId);

                var bufferStr = formatter.Format(it.AssetFileId);
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(bufferStr);

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(it.RelativePath);

                bufferStr = formatter.Format(it.SizeInBytes);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(bufferStr);

                ImGui.TableNextColumn();
                if (it.ContentHash != null)
                    ImGui.Text(it.ContentHash);

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }
}