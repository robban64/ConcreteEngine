using System.Text;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Widgets;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.EditorConsts;

namespace ConcreteEngine.Editor.Panels.Assets;

internal sealed class AssetListPanel : EditorPanel
{
    private static SearchStringUtf8 _inputUtf8;

    private AssetKind _selectedKind;

    private readonly ClipDrawer _clipDrawer;
    private readonly EnumCombo<AssetKind> _assetCombo = EnumCombo<AssetKind>.MakeFromCache(defaultName: "All");

    private readonly AssetController _controller;

    private readonly AssetId[] _assetIds = new AssetId[AssetCapacity];
    private int _assetCount;


    public AssetListPanel(PanelContext context, AssetController controller) : base(PanelId.AssetList, context)
    {
        _controller = controller;
        _clipDrawer = new ClipDrawer(DrawListItem);
    }

    public override void Enter()
    {
        if (_assetCount == 0) TriggerSearch();
    }

    private void OnCategoryChange(AssetKind kind)
    {
        if (_selectedKind == kind) return;
        _selectedKind = kind;
        TriggerSearch();
    }

    public override void Draw(in FrameContext ctx)
    {
        const ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.CharsNoBlank;
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.65f);

        if (ImGui.InputText("##search-asset"u8, ref _inputUtf8.GetInputRef(), SearchStringUtf8.Length, inputFlags))
            TriggerSearch();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.35f);

        if (_assetCombo.Draw((int)_selectedKind, out var kind))
            OnCategoryChange(kind);

        if (_selectedKind != AssetKind.Unknown)
            DrawTable(in ctx);
    }

    private void DrawTable(in FrameContext ctx)
    {
        var count = _assetCount;

        var layout = TextLayout.Make()
            .TitleSeparator(ref WriteFormat.WriteTitleId(ctx.Writer, "Assets"u8, count), padUp: false);


        if (ImGui.BeginTable("asset-list"u8, 3, GuiTheme.TableFlags))
        {
            layout.Row("Type"u8).Row("Id"u8).RowStretch("Name"u8);

            _clipDrawer.Draw(count, GuiTheme.ListPaddedRowHeight, in ctx);

            ImGui.EndTable();
        }
    }

    private void DrawListItem(int i, in FrameContext ctx)
    {
        var id = _assetIds[i];
        var selected = id == ctx.SelectedAssetId;
        var sw = ctx.Writer;

        _controller.GetAssetItem(id, out var it);

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


    private void TriggerSearch()
    {
        _assetIds.AsSpan(0, _assetCount).Clear();

        var searchStr = _inputUtf8.GetSearchString(out var key, out var mask);

        var search = new SearchPayload<AssetId>(searchStr, _assetIds, key, mask);
        var filter = SearchFilter.MakeAsset(_selectedKind);

        _assetCount = _controller.FilterQuery(in search, filter,
            static (in search, filter, in it) =>
            {
                return SearchQuery(in search, filter, in it);
            });
    }

    private static bool SearchQuery(in SearchPayload<AssetId> search, SearchFilter filter, in AssetItem it)
    {
        if (search.SearchKey > 0 && (it.NameKey & search.SearchMask) != search.SearchKey)
            return false;

        if (search.SearchString.Length > 8 && !it.Name.StartsWith(search.SearchString, StringComparison.Ordinal))
            return false;

        return true;
    }
}