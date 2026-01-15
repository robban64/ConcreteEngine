using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using static ConcreteEngine.Editor.UI.GuiUtils;

namespace ConcreteEngine.Editor.Components.Assets;

internal sealed class DrawAssetList
{
    public const int RowHeight = 32;
    public const int PaddedRowHeight = 32 + 4;
    public const int ColumnWidth = 36;

    private readonly Action<int> _drawRowDel;
    private readonly AssetsComponent _component;

    private  DrawContext DrawCtx => _component.DrawCtx;

    public DrawAssetList(AssetsComponent component)
    {
        _component = component;
        _drawRowDel = DrawListItem;
    }

    private void CategoryChanged(AssetState state, AssetKind kind)
    {
        if (kind == state.ShowKind) return;
        state.ShowKind = kind;

        if (kind == AssetKind.Unknown) state.ResetState();
    }

    public void DrawTypeSelector(AssetState state)
    {
        var category = state.ShowKind;
        var length = state.AssetKindLength;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);
        if (!ImGui.BeginCombo("##assetTypeSelector"u8, category.ToTextUtf8(), ImGuiComboFlags.HeightLargest))
            return;

        for (var i = 0; i < length; i++)
        {
            var isSelected = i == (int)category;
            var kind = (AssetKind)i;

            if (ImGui.Selectable(kind.ToTextUtf8(), isSelected, ImGuiSelectableFlags.None, Vector2.Zero))
                CategoryChanged(state, kind);

            if (isSelected)
                ImGui.SetItemDefaultFocus();
        }

        ImGui.EndCombo();
    }

    public void DrawList(AssetState state)
    {
        var len = state.GeAssetSpan().Length;
        if (len == 0) return;
        GuiActions.ForVisible(len, PaddedRowHeight, _drawRowDel);
    }


    private void DrawListItem(int i)
    {
        var state  = _component.State;
        var it = state.GeAssetSpan()[i];
        var za = DrawCtx.GetWriter();
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

        ImGui.PushID(it.Id);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        NextCenterAlignText(it.Kind.ToShortTextUtf8(), PaddedRowHeight);
        ImGui.TextColored(it.Kind.ToColor(), it.Kind.ToShortTextUtf8());

        ImGui.TableNextColumn();

        var isSelected = it.Id == state.SelectedId;
        var spanText = za.AppendEnd(it.Id).AsSpan();
        if (Selectable(spanText, isSelected, RowHeight, ColumnWidth))
            _component.TriggerSelection(it.Id);

        za.Clear();
        ImGui.TableNextColumn();
        CenterAlignTextVertical(za.AppendEnd(it.Name).AsSpan(), PaddedRowHeight);

        za.Clear();

        ImGui.PopID();
        ImGui.PopStyleVar();
    }
}