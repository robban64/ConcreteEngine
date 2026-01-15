using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components.Assets;

internal sealed class DrawAssetList(AssetsComponent component)
{
    public const int RowHeight = 32;
    public const int PaddedRowHeight = 32 + 4;
    public const int ColumnWidth = 36;

    private readonly SelectionCombo<byte> _combo = new(
        EnumCache<AssetKind>.GetNames().ToArray(),
        MemoryMarshal.Cast<AssetKind, byte>(EnumCache<AssetKind>.GetValues()).ToArray()
    );

    private void CategoryChanged(AssetState state, AssetKind kind)
    {
        if (kind == state.ShowKind) return;
        state.ShowKind = kind;
        _combo.Sync((byte)kind);
        if (kind == AssetKind.Unknown) state.ResetState();
    }

    public void DrawTypeSelector(AssetState state, ref FrameContext ctx)
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);
        if (_combo.Draw("##assetTypeSelector"u8, out var kind, ref ctx.Sw))
        {
            CategoryChanged(state, (AssetKind)kind);
        }
    }

    public void DrawListItem(int i, ref SpanWriter writer)
    {
        var state = component.State;
        var it = state.GeAssetSpan()[i];
        var selected = it.Id == state.SelectedId;

        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

        ImGui.PushID(it.Id);
        ImGui.TableNextRow();

        new TextLayout(PaddedRowHeight, TextAlignMode.Center)
            .DrawColumnColor(it.Kind.ToColor(), it.Kind.ToShortTextUtf8())
            .SelectableColumn(writer.Write(it.Id.Value), selected, ColumnWidth, out var hasClicked)
            .DrawColumn(writer.Write(it.Name));

        if (hasClicked) component.TriggerSelection(it.Id);

        ImGui.PopID();
        ImGui.PopStyleVar();
    }
}