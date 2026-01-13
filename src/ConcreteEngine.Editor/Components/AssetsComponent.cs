using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Editor.Components.Draw;
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
    private const int ColumnWidth = 36;

    private int _popupInput;

    public override void DrawLeft(AssetState state, in FrameContext ctx)
    {
        ImGui.SeparatorText("Asset Store"u8);

        var za = ctx.GetWriter();
        DrawAssets.DrawAssetTypeSelector(state, state.AssetKindLength, ref za);

        if (state.SelectedKind == AssetKind.Unknown) return;

        if (!ImGui.BeginTable("##asset_store_object_tbl"u8, 3, GuiTheme.TableFlags)) return;

        ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();
        
        DrawList(state, ref za);

        ImGui.EndTable();
    }

    private void DrawList(AssetState state, ref ZaUtf8SpanWriter za)
    {
        var assetSpan = state.Assets;
        if (assetSpan.Length == 0) return;

        za.Clear();

        var clipper = new ImGuiListClipper();
        clipper.Begin(assetSpan.Length, RowHeight);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, len = clipper.DisplayEnd;
            if ((uint)len > assetSpan.Length) throw new IndexOutOfRangeException();

            for (var i = start; i < len; i++)
                DrawListItem(state, assetSpan[i], ref za);
        }

        clipper.End();
    }
    

    private void DrawListItem(AssetState state, IAsset it, ref ZaUtf8SpanWriter za)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

        ImGui.PushID(za.AppendEnd(it.Id).AsSpan());
        ImGui.TableNextRow(ImGuiTableRowFlags.None, RowHeight);
        za.Clear();

        ImGui.TableNextColumn();
        NextCenterAlignText(it.Kind.ToShortTextUtf8(), RowHeight + 4);
        DrawAssets.DrawAssetKindTag(it.Kind);
        
        ImGui.TableNextColumn();
        bool isSelected = it.Id == state.SelectedId;
        var spanText = za.AppendEnd(it.Id).AsSpan();
        if (ObjectSelectable(spanText, isSelected, RowHeight, ColumnWidth))
            TriggerEvent(EventKey.SelectionChanged, it.Id);


        za.Clear();
        ImGui.TableNextColumn();
        CenterAlignTextVertical(za.AppendEnd(it.Name).AsSpan(),RowHeight+ 4);
 
        za.Clear();

        ImGui.PopID();
        ImGui.PopStyleVar();

    }

}