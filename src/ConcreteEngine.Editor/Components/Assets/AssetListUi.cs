using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Core;
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
        if (kind == state.SelectAssetKind) return;
        state.SelectAssetKind = kind;
        if (kind == AssetKind.Unknown) state.ResetState();
    }

    public void DrawTypeSelector(AssetState state, ref FrameContext ctx)
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);

        var combo = new EnumCombo<AssetKind>((int)state.SelectAssetKind);
        if(combo.Draw(ref ctx.Sw,"##asset-combo"u8, out var kind))
            CategoryChanged(state, kind);

    }

    public void DrawListItem(int i, AssetProxy? proxy, ref FrameContext ctx)
    {
        var state = component.State;

        var it = EngineController.AssetController.GetAssetSpan(state.SelectAssetKind)[i];
        var selected = it.Id == proxy?.Asset.Id;

        ImGui.PushID(it.Id);
        ImGui.TableNextRow();

        new TextLayout(PaddedRowHeight, TextAlignMode.Center)
            .ColumnColor(it.Kind.ToColor(), it.Kind.ToShortTextUtf8())
            .SelectableColumn(ctx.Sw.Write(it.Id.Value), selected, ColumnWidth, out var hasClicked)
            .WithLayout(TextAlignMode.VerticalCenter)
            .Column(ctx.Sw.Write(it.Name));

        if (hasClicked) component.TriggerSelection(it.Id);

        ImGui.PopID();
    }
}