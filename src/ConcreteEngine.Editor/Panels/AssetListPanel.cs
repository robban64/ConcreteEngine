using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class AssetListPanel : EditorPanel
{
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

    private readonly ClipDrawer<IAsset> _clipDrawer;
    private readonly EnumCombo<AssetKind> _assetCombo = EnumCombo<AssetKind>.MakeFromCache(defaultName: "All");

    private readonly AssetController _controller;

    public AssetListPanel(PanelContext context, AssetController controller) : base(PanelId.AssetList, context)
    {
        _controller = controller;
        _clipDrawer = new ClipDrawer<IAsset>(DrawListItem);
    }


    public override void Draw(in FrameContext ctx)
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);
        if (_assetCombo.Draw((int)SelectedKind, ctx.Writer, out var kind))
            SelectedKind = kind;


        if (SelectedKind == AssetKind.Unknown) return;

        var span = _controller.GetAssetSpan(SelectedKind);
        var layout = TextLayout.Make()
            .TitleSeparator(ref WriteFormat.WriteTitleId(ctx.Writer, "Assets"u8, span.Length), padUp: false);

        if (ImGui.BeginTable("##asset-list"u8, 3, GuiTheme.TableFlags))
        {
            layout.Row("Type"u8).Row("Id"u8).RowStretch("Name"u8);

            _clipDrawer.Draw(span.Length, GuiTheme.ListPaddedRowHeight, span, in ctx);

            ImGui.EndTable();
        }
    }


    private void DrawListItem(int i, IAsset it, in FrameContext ctx)
    {
        var id = it.Id;
        var selected = id == ctx.SelectedAssetId;

        var sw = ctx.Writer;

        ImGui.PushID(id);
        ImGui.TableNextRow();

        new TextLayout(GuiTheme.ListRowHeight, TextAlignMode.Center)
            .ColumnColor(in _selectedColor, it.Kind.ToShortTextUtf8())
            .SelectableColumn(ref sw.Write(id.Value), selected, GuiTheme.IdColWidth, out var hasClicked)
            .WithLayout(TextAlignMode.VerticalCenter)
            .Column(ref sw.Write(it.Name));

        if (hasClicked) Context.EnqueueEvent(new AssetEvent(id));

        ImGui.PopID();
    }
}