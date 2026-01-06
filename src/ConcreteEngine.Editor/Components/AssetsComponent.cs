using System.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;
using static ConcreteEngine.Editor.Utils.GuiUtils;

namespace ConcreteEngine.Editor.Components;

internal static class AssetsComponent
{
    private const int RowHeight = 32;
    private static readonly Vector2 BtnSize = new(RowHeight, 22);

    private static readonly string[] AssetKindNames = ["None", "Shader", "Model", "Texture", "Material"];

    public static EditorAssetResource[] Assets = [];
    public static EditorFileAssetModel[] FileAssets = [];

    private static AssetKind _kind;
    private static int _popupInput;

    private static ModelStateContext Context => ModelManager.AssetStateContext;

    public static void ResetState(bool clearTypeSelection = false)
    {
        if (clearTypeSelection) _kind = AssetKind.Unknown;
        FileAssets = [];
        Assets = [];
    }

    public static void OnEnter()
    {
        if(_kind == AssetKind.Unknown) return;
        Context.TriggerEvent(EventKey.CategoryChanged, _kind);

    }

    private static void OnCategoryChanged(AssetKind kind)
    {
        if (kind == _kind) return;
        _kind = kind;
        if (kind == AssetKind.Unknown)
        {
            ResetState();
            return;
        }
        Context.TriggerEvent(EventKey.CategoryChanged, kind);
    }

    private static void OnSelectionChanged(EditorAssetResource? asset) =>
        Context.TriggerEvent(EventKey.SelectionChanged, asset);

    public static void Draw()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoBordersInBody |
                                      ImGuiTableFlags.ScrollY;

        ImGui.SeparatorText("Asset Store"u8);
        
        Span<byte> buffer = stackalloc byte[64];
        var za = ZaUtf8SpanWriter.Create(buffer);
        DrawAssetTypeSelector(za);

        if (_kind == AssetKind.Unknown) return;
        if (!ImGui.BeginTable("##asset_store_object_tbl"u8, 3, flags)) return;

        ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 36);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##asset-tbl-separator-col"u8, ImGuiTableColumnFlags.WidthFixed, RowHeight);

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

        ImGui.TableNextColumn();
        CenterAlignTextHorizontal("Id"u8);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Name"u8);

        ImGui.TableNextColumn();
        CenterAlignTextHorizontal(" "u8);

        ImGui.TableNextColumn();


        DrawList(buffer);

        ImGui.EndTable();
    }

    private static void DrawList(Span<byte> buffer)
    {
        if(Assets.Length == 0) return;
        
        var assetSpan = Assets.AsSpan();

        var rowHeight = RowHeight + (ImGui.GetStyle().CellPadding.Y * 2);
        var clipper = new ImGuiListClipper();
        clipper.Begin(assetSpan.Length, rowHeight);

        var za = ZaUtf8SpanWriter.Create(buffer);

        while (clipper.Step())
        {
            int start = clipper.DisplayStart, len = clipper.DisplayEnd;
            if ((uint)start > len || (uint)len > assetSpan.Length)
                throw new IndexOutOfRangeException();

            for (int i = start; i < len; i++)
                DrawListItem(rowHeight, assetSpan[i], za);
        }

        clipper.End();
    }

    private static void DrawListItem(float rowHeight, EditorAssetResource it, ZaUtf8SpanWriter za)
    {
        za.Clear();
        ImGui.PushID(za.Append(it.Id.Identifier).AppendEndOfBuffer().AsSpan());
        ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);
        za.Clear();

        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(za.Append(it.Id.Identifier).AppendEndOfBuffer().AsSpan());
        za.Clear();

        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(za.AppendEnd(it.Name).AsSpan());
        za.Clear();

        ImGui.TableNextColumn();

        var idSpan = za.Append(it.Id.Identifier).AppendEndOfBuffer().AsSpan();
        if (ImGui.Button(">"u8, BtnSize))
        {
            if (_popupInput < 1) _popupInput = 1;
            OnSelectionChanged(it);

            var itemMin = ImGui.GetItemRectMin();
            var itemMax = ImGui.GetItemRectMax();
            ImGui.SetNextWindowPos(new Vector2(itemMax.X + 16, itemMin.Y - 32));
            ImGui.OpenPopup(idSpan);
        }

        if (ImGui.IsPopupOpen(idSpan))
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f));


        if (ImGui.BeginPopup(idSpan))
        {
            DrawAssetFilePopupContent(it, za);
            ImGui.EndPopup();
            ImGui.PopStyleVar();
        }

        za.Clear();


        ImGui.PopID();
    }

    private static void DrawAssetTypeSelector(ZaUtf8SpanWriter za)
    {
        var category = _kind;
        var categoryNames = AssetKindNames.AsSpan();


        var currentLabel = categoryNames[(int)category];
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);

        za.Clear();
        if (ImGui.BeginCombo("##assetTypeSelector"u8, currentLabel, ImGuiComboFlags.HeightLargest))
        {

            for (var i = 0; i < categoryNames.Length; i++)
            {
                var isSelected = i == (int)_kind;

                if (ImGui.Selectable(za.AppendEnd(categoryNames[i]).AsSpan(), isSelected, ImGuiSelectableFlags.None,
                        Vector2.Zero))
                {
                    OnCategoryChanged((AssetKind)i);
                }

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
                
                za.Clear();
            }


            ImGui.EndCombo();
        }

    }


    private static void DrawAssetFilePopupContent(EditorAssetResource asset, ZaUtf8SpanWriter za)
    {
        za.Clear();
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(2, 2));
        ImGui.SeparatorText(za.AppendEnd(asset.Name).AsSpan());

        if (ImGui.BeginTable("##asset_detail_object_tbl"u8, 4,
                ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
        {
            ImGui.PushID(asset.Id);
            ImGui.TableSetupColumn(asset.ResourceName, ImGuiTableColumnFlags.WidthFixed, 34);
            ImGui.TableSetupColumn("Gen"u8, ImGuiTableColumnFlags.WidthFixed, 34);
            ImGui.TableSetupColumn("Core"u8, ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn(asset.SpecialName, ImGuiTableColumnFlags.WidthFixed);

            ImGui.TableHeadersRow();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(za.Append(asset.ResourceId).AppendEndOfBuffer().AsSpan());
            za.Clear();

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(za.Append(asset.Generation).AppendEndOfBuffer().AsSpan());
            za.Clear();

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(StrUtils.BoolToYesNoShort(asset.IsCoreAsset));

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(za.Append(asset.SpecialValue).AppendEndOfBuffer().AsSpan());
            ImGui.PopID();
            za.Clear();

            ImGui.EndTable();
        }

        if (FileAssets.Length > 0)
            DrawFilesTable(za);

        if (asset.HasActions)
        {
            ImGui.Separator();
            if (ImGui.Button("Reload"u8, new Vector2(72, 28)))
            {
                ModelManager.AssetStateContext.TriggerEvent(EventKey.SelectionAction, asset);
                ImGui.CloseCurrentPopup();
            }
        }


        ImGui.PopStyleVar();
        return;

        static void DrawFilesTable(ZaUtf8SpanWriter za)
        {
            DrawSectionHeader("Files"u8);
            if (!ImGui.BeginTable("##asset_store_files_tbl"u8, 4, ImGuiTableFlags.Borders)) return;
            ImGui.TableSetupColumn("ID"u8, ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Path"u8, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Size"u8, ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Hash"u8, ImGuiTableColumnFlags.WidthFixed);

            ImGui.TableHeadersRow();


            foreach (var it in FileAssets)
            {
                za.Clear();
                ImGui.TableNextRow();
                ImGui.PushID(za.Append(it.AssetFileId).AppendEndOfBuffer().AsSpan());
                za.Clear();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(za.Append(it.AssetFileId).AppendEndOfBuffer().AsSpan());
                za.Clear();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(za.AppendEnd(it.RelativePath).AsSpan());
                za.Clear();

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(za.Append(it.SizeInBytes).AppendEndOfBuffer().AsSpan());
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