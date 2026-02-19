using System.Numerics;
using System.Text;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Widgets;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.EditorConsts;

namespace ConcreteEngine.Editor.Panels.Assets;

internal sealed class AssetListPanel : EditorPanel
{
    private static SearchStringUtf8 _inputUtf8;

    // private readonly EnumCombo<AssetKind> _assetCombo = EnumCombo<AssetKind>.MakeFromCache(defaultName: "All");
    private readonly ComboField _assetCombo =
        ComboField.MakeFromEnumCache<AssetKind>("##asset-combo", "None", null, null);

    private readonly ClipDrawer _clipDrawer;

    private readonly AssetController _controller;

    private readonly AssetId[] _assetIds = new AssetId[AssetCapacity];
    private int _assetCount;

    private String16Utf8 _selectedKindText;
    private AssetKind _selectedKind;

    public AssetListPanel(PanelContext context, AssetController controller) : base(PanelId.AssetList, context)
    {
        _controller = controller;
        _clipDrawer = new ClipDrawer(DrawListItem);
    }

    public override void Enter()
    {
        if (_assetCount == 0) TriggerSearch();
    }

    private void OnCategoryChange()
    {
        _selectedKind = (AssetKind)_assetCombo.Value;
        if (_selectedKind > 0)
            _selectedKindText = new String16Utf8(_selectedKind.ToShortText());

        TriggerSearch();
    }

    public override void Draw(in FrameContext ctx)
    {
        DrawHeader();

        if (_selectedKind == AssetKind.Unknown) return;

        ImGui.SeparatorText(ref WriteFormat.WriteTitleId(ctx.Writer, "Assets"u8, _assetCount));

        if (!ImGui.BeginTable("asset-table"u8, 4, GuiTheme.TableFlags)) return;

        TextLayout.Make().Row("Type"u8).Row("Id"u8).RowStretch("Name"u8).Row("-"u8, GuiTheme.ListRowHeight);

        DurationProfileTimer.Default.Begin();
        _clipDrawer.Draw(_assetCount, GuiTheme.ListPaddedRowHeight, in ctx);
        DurationProfileTimer.Default.EndPrintSimple();
        ImGui.EndTable();
    }

    private void DrawHeader()
    {
        const ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.CharsNoBlank;
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;

        ImGui.SetNextItemWidth(width * 0.62f);
        if (ImGui.InputText("##search-asset"u8, ref _inputUtf8.GetInputRef(), SearchStringUtf8.Length, inputFlags))
            TriggerSearch();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.38f);
        if (_assetCombo.DrawComponent())
            OnCategoryChange();
    }


    private unsafe void DrawListItem(int i, in FrameContext ctx)
    {
        var id = _assetIds[i];
        var selected = id == ctx.SelectedAssetId;
        var sw = ctx.Writer;

        var name = _controller.GetAssetName(id);

        ImGui.PushID(id);
        ImGui.TableNextRow();

        TextLayout.Make(GuiTheme.ListRowHeight, TextAlignMode.Center)
            .ColumnColor(in StyleMap.GetAssetColor(_selectedKind), ref _selectedKindText.GetRef())
            .SelectableColumn(ref sw.Write(id), selected, GuiTheme.IdColWidth, out var hasClicked)
            .WithLayout(TextAlignMode.VerticalCenter)
            .Column(ref sw.Write(name));

        ImGui.TableNextColumn();

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Vector2.Zero);
        if (_selectedKind == AssetKind.Texture)
        {
            var textureId = _controller.GetTextureId(id);
            var texPtr = Context.GetTextureRefPtr(textureId);
            ImGui.Image(*texPtr.Handle, new Vector2(32, 32));
        }

        ImGui.PopStyleVar();

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

    private static bool SearchQuery(in SearchPayload<AssetId> search, SearchFilter filter, in AssetQueryItem it)
    {
        if (search.SearchKey > 0 && (it.NameKey & search.SearchMask) != search.SearchKey)
            return false;

        if (search.SearchString.Length > 8 && !it.Name.StartsWith(search.SearchString, StringComparison.Ordinal))
            return false;

        return true;
    }
}