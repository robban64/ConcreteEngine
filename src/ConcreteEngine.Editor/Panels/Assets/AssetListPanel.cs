using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Assets;

internal sealed class AssetListPanel : EditorPanel
{
    private AssetKind _selectedKind;

    private readonly ClipDrawer<IAsset> _clipDrawer;
    private readonly EnumCombo<AssetKind> _assetCombo = EnumCombo<AssetKind>.MakeFromCache(defaultName: "All");

    private readonly AssetController _controller;

    private static readonly NativeArray<byte> InputBuffer = new(32);

    public AssetListPanel(PanelContext context, AssetController controller) : base(PanelId.AssetList, context)
    {
        _controller = controller;
        _clipDrawer = new ClipDrawer<IAsset>(DrawListItem);
    }


    public override unsafe void Draw(in FrameContext ctx)
    {
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.65f);

        if (ImGui.InputText("##input"u8, InputBuffer, 16, ImGuiInputTextFlags.CharsNoBlank)) ;
            //TriggerSearch();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.35f);
        
        if (_assetCombo.Draw((int)_selectedKind, ctx.Writer, out var kind))
            _selectedKind = kind;

        if (_selectedKind != AssetKind.Unknown)
            DrawTable(in ctx);

    }

    private void DrawTable(in FrameContext ctx)
    {
        var span = _controller.GetAssetSpan(_selectedKind);
        var layout = TextLayout.Make()
            .TitleSeparator(ref WriteFormat.WriteTitleId(ctx.Writer, "Assets"u8, span.Length), padUp: false);

        if (ImGui.BeginTable("asset-list"u8, 3, GuiTheme.TableFlags))
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
            .ColumnColor(StyleMap.GetAssetColor(_selectedKind), it.Kind.ToShortTextUtf8())
            .SelectableColumn(ref sw.Write(id), selected, GuiTheme.IdColWidth, out var hasClicked)
            .WithLayout(TextAlignMode.VerticalCenter)
            .Column(ref sw.Write(it.Name));

        if (hasClicked) Context.EnqueueEvent(new AssetEvent(id));

        ImGui.PopID();
    }
}