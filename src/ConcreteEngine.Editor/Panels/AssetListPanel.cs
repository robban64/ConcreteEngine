using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class AssetListPanel : EditorPanel
{
    public const int RowHeight = 32;
    public const int PaddedRowHeight = 32 + 4;
    public const int ColumnWidth = 36;

    private Color4 _selectedColor;

    public AssetKind SelectedKind
    {
        get;
        set
        {
            if (SelectedKind == value) return;
            field = value;
            _selectedColor = field.ToColor();
        }
    }

    private readonly EnumCombo<AssetKind> _assetCombo = EnumCombo<AssetKind>.MakeFromCache();

    private readonly ClipDrawer<IAsset> _clipDrawer;

    public AssetListPanel() : base(PanelId.AssetList)
    {
        _clipDrawer = new ClipDrawer<IAsset>(DrawListItem);
    }


    public override void Draw(ref FrameContext ctx)
    {
        ImGui.SeparatorText("Asset Store"u8);

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);
        if (_assetCombo.Draw((int)SelectedKind, "##asset-combo"u8, out var kind))
            SelectedKind = kind;

        if (SelectedKind == AssetKind.Unknown) return;
        if (!ImGui.BeginTable("##asset-list"u8, 3, GuiTheme.TableFlags)) return;

        TextLayout.Make().Row("Type"u8).Row("Id"u8).RowStretch("Name"u8);
        ImGui.TableHeadersRow();

        var span = EngineController.AssetController.GetAssetSpan(SelectedKind);
        _clipDrawer.Draw(span.Length, PaddedRowHeight, span, ref ctx);

        ImGui.EndTable();
    }


    private void DrawListItem(int i, IAsset it, ref FrameContext ctx)
    {
        var selected = it.Id == Context.SelectedAssetId;

        ImGui.PushID(it.Id);
        ImGui.TableNextRow();

        new TextLayout(PaddedRowHeight, TextAlignMode.Center)
            .ColumnColor(in _selectedColor, it.Kind.ToShortTextUtf8())
            .SelectableColumn(ctx.Sw.Write(it.Id.Value), selected, ColumnWidth, out var hasClicked)
            .WithLayout(TextAlignMode.VerticalCenter)
            .Column(ctx.Sw.Write(it.Name));

        if (hasClicked) Context.EnqueueEvent(new AssetEvent(it.Id));

        ImGui.PopID();
    }
}