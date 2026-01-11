using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;
using static ConcreteEngine.Editor.Utils.GuiUtils;

namespace ConcreteEngine.Editor.Components;

internal sealed class AssetsComponent : EditorComponent<AssetState>
{
    private const int RowHeight = 32;

    private readonly int _assetKindLength = EnumCache<AssetKind>.Count;

    private int _popupInput;
    
    private static void CategoryChanged(AssetState state, AssetKind kind)
    {
        if (kind == state.SelectedKind) return;
        state.SelectedKind = kind;

        if (kind == AssetKind.Unknown) state.ResetState();
    }


    public override void DrawLeft(AssetState state, in FrameContext ctx)
    {
        ImGui.SeparatorText("Asset Store"u8);

        var za = ctx.GetWriter();
        DrawAssetTypeSelector(state, ref za);

        if (state.SelectedKind == AssetKind.Unknown) return;
        if (!ImGui.BeginTable("##asset_store_object_tbl"u8, 3, GuiTheme.TableFlags)) return;

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

        DrawList(state, ref za);

        ImGui.EndTable();
    }
    
    private void DrawAssetTypeSelector(AssetState state, ref ZaUtf8SpanWriter za)
    {
        var category = state.SelectedKind;

        var currentLabel = category.ToTextUtf8();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);

        za.Clear();
        if (ImGui.BeginCombo("##assetTypeSelector"u8, currentLabel, ImGuiComboFlags.HeightLargest))
        {
            for (var i = 0; i < _assetKindLength; i++)
            {
                var isSelected = i == (int)category;
                var kind = (AssetKind)i;

                if (ImGui.Selectable(kind.ToTextUtf8(), isSelected, ImGuiSelectableFlags.None, Vector2.Zero))
                    CategoryChanged(state, kind);

                if (isSelected)
                    ImGui.SetItemDefaultFocus();

                za.Clear();
            }


            ImGui.EndCombo();
        }
    }


    private void DrawList(AssetState state, ref ZaUtf8SpanWriter za)
    {
        var assetSpan = state.Assets;
        if (assetSpan.Length == 0) return;

        za.Clear();

        var rowHeight = RowHeight + (ImGui.GetStyle().CellPadding.Y * 2);
        var clipper = new ImGuiListClipper();
        clipper.Begin(assetSpan.Length, rowHeight);

        while (clipper.Step())
        {
            int start = clipper.DisplayStart, len = clipper.DisplayEnd;
            if ((uint)start > len || (uint)len > assetSpan.Length)
                throw new IndexOutOfRangeException();

            for (int i = start; i < len; i++)
                DrawListItem(state, rowHeight, assetSpan[i], ref za);
        }

        clipper.End();
    }

    private void DrawListItem(AssetState state, float rowHeight, AssetObject it, ref ZaUtf8SpanWriter za)
    {
        var spanText = za.AppendEnd(it.Id).AsSpan();
        ImGui.PushID(spanText);
        ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);

        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(spanText);
        za.Clear();

        spanText = za.AppendEnd(it.Name).AsSpan();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(spanText);
        za.Clear();

        ImGui.TableNextColumn();

        spanText = za.AppendEnd(it.Id).AsSpan();
        if (ImGui.Button(">"u8, new Vector2(RowHeight, 22)))
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

        za.Clear();

        ImGui.PopID();
    }

    private static void DrawAssetFilePopupContent(AssetState state, AssetObject asset, ref ZaUtf8SpanWriter za)
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
            DrawSectionHeader("Files"u8);
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