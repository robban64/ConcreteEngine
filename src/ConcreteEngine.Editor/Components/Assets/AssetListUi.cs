using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components.Assets;

internal sealed class AssetListUi(AssetsComponent component)
{
    public const int RowHeight = 32;
    public const int PaddedRowHeight = 32 + 4;
    public const int ColumnWidth = 36;

    private void CategoryChanged(AssetState state, AssetKind kind)
    {
        if (kind == state.ShowKind) return;
        state.ShowKind = kind;
        if (kind == AssetKind.Unknown) state.ResetState();
    }

    public void DrawTypeSelector(AssetState state, ref FrameContext ctx)
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);

        var combo = new EnumCombo<AssetKind>((int)state.ShowKind);
        if(combo.Draw("##asset-combo", out var kind))
            CategoryChanged(state, kind);

    }

    public void DrawListItem(int i, ref SpanWriter writer)
    {
        var state = component.State;
        var it = state.GeAssetSpan()[i];
        var selected = it.Id == state.SelectedId;

        ImGui.PushID(it.Id);
        ImGui.TableNextRow();

        new TextLayout(PaddedRowHeight, TextAlignMode.Center)
            .NextColumnColor(it.Kind.ToColor(), it.Kind.ToShortTextUtf8())
            .SelectableColumn(writer.Write(it.Id.Value), selected, ColumnWidth, out var hasClicked)
            .WithLayout(TextAlignMode.VerticalCenter)
            .NextColumn(writer.Write(it.Name));

        if (hasClicked) component.TriggerSelection(it.Id);

        ImGui.PopID();
    }
}