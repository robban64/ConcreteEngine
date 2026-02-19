using System.Numerics;
using System.Runtime.CompilerServices;
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
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.EditorConsts;

namespace ConcreteEngine.Editor.Panels.Assets;

internal struct ListFilterItem(int value, string label)
{
    public String16Utf8 Label = new(label);
    public int Value = value;
}

internal sealed class AssetListPanel : EditorPanel
{
    private static SearchStringUtf8 _inputUtf8;

    private readonly ListFilterItem[] TextureFilter =
    [
        new((int)TextureKind.Texture2D, "Texture2D"), new((int)TextureKind.CubeMap, "CubeMap")
    ];

    private readonly ListFilterItem[] ModelFilter =
        [new(1, "Static"), new(2, "Animated")];

    private readonly ComboField _assetCombo =
        ComboField.MakeFromEnumCache<AssetKind>("##asset-combo", "None", null, null);

    //private readonly ClipDrawer _clipDrawer;

    private readonly AssetController _controller;

    private readonly AssetId[] _assetIds = new AssetId[AssetCapacity];
    private int _assetCount;

    private String16Utf8 _selectedKindText;
    private AssetKind _selectedKind;
    private int _selectedFilter = -1;

    public AssetListPanel(PanelContext context, AssetController controller) : base(PanelId.AssetList, context)
    {
        _controller = controller;
        //_clipDrawer = new ClipDrawer(DrawListItem);
    }

    public override void Enter()
    {
        if (_assetCount == 0) TriggerSearch();
    }

    private void OnCategoryChange()
    {
        _selectedKind = (AssetKind)_assetCombo.Value;
        if (_selectedKind > 0)
            _selectedKindText = new String16Utf8(_selectedKind.ToText());
        else
            _selectedKindText = new String16Utf8(AssetKind.Unknown.ToText());

        _selectedFilter = -1;

        TriggerSearch();
    }

    public override void Draw(in FrameContext ctx)
    {
        DrawHeader();

        if (_selectedKind == AssetKind.Unknown) return;

        ImGui.SeparatorText(ref WriteFormat.WriteTitleId(ctx.Writer, _selectedKindText.GetStringSpan(), _assetCount));
        DrawAssetList(in ctx);
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

        //
        var kind = _selectedKind;
        if (kind != AssetKind.Texture && kind != AssetKind.Model) return;

        var filters = kind == AssetKind.Model ? ModelFilter : TextureFilter;
        var preview = _selectedFilter < 0 ? "Filter"u8 : filters[_selectedFilter].Label.GetStringSpan();
        var changed = false;
        if (ImGui.BeginCombo("##search-filter"u8, preview))
        {
            for (var i = 0; i < filters.Length; i++)
            {
                var isSelected = i == _selectedFilter;
                if (ImGui.Selectable(ref filters[i].Label.GetRef(), isSelected))
                {
                    _selectedFilter = i;
                    changed = true;
                }

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }

        if (changed)
        {
            TriggerSearch();
        }
    }

    private unsafe void DrawAssetList(in FrameContext ctx)
    {
        if (!ImGui.BeginTable("asset-list"u8, 3, GuiTheme.TableFlags)) return;

        ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);

        var assetIds = _assetIds;
        var kind = _selectedKind;
        var iconName = _selectedKind switch
        {
            AssetKind.Shader => IconNames.Code,
            AssetKind.Model => IconNames.Box,
            AssetKind.Material => IconNames.Circle,
            AssetKind.Texture => IconNames.Image,
            _ => throw new ArgumentOutOfRangeException()
        };

        byte* icon = stackalloc byte[3];
        UtfText.FormatChar(icon, iconName);

        var color = StyleMap.GetAssetColor(kind);

        var clipper = new ImGuiListClipper();
        clipper.Begin(_assetCount, GuiTheme.ListPaddedRowHeight);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, end = clipper.DisplayEnd;
            for (var i = start; i < end; i++)
            {
                var id = assetIds[i];
                ImGui.PushID(id);
                DrawTableRow(id, kind, ref icon[0], in color, in ctx);
                ImGui.PopID();
            }
        }

        clipper.End();
        ImGui.EndTable();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawTableRow(AssetId id, AssetKind kind, ref byte icon, in Color4 color, in FrameContext ctx)
    {
        const ImGuiSelectableFlags
            selectFlags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        var selected = id == ctx.SelectedAssetId;
        var sw = ctx.Writer;
        var name = _controller.GetAssetName(id);

        ImGui.TableNextRow();
        var cellTop = ImGui.GetCursorPosY();

        ImGui.TableNextColumn();
        if (ImGui.Selectable("##select"u8, selected, selectFlags, new Vector2(0, GuiTheme.ListRowHeight)))
        {
            Context.EnqueueEvent(new AssetEvent(id));
        }

        if (kind != AssetKind.Texture || !DrawTextureThumbnail(id, cellTop))
        {
            GuiLayout.NextAlignTextVerticalTop(cellTop, GuiTheme.ListRowHeight, GuiTheme.IconMediumSize);
            GuiTheme.PushFontIconMedium();
            ImGui.Text(ref icon);
            ImGui.PopFont();
        }


        ImGui.TableNextColumn();
        GuiLayout.NextAlignTextVerticalTop(cellTop, GuiTheme.ListRowHeight);
        ImGui.TextColored(color, ref sw.Start('[').Append(id).Append(']').End());

        ImGui.TableNextColumn();
        GuiLayout.NextAlignTextVerticalTop(cellTop, GuiTheme.ListRowHeight);
        ImGui.TextUnformatted(ref sw.Write(name));
    }


    
    private unsafe bool DrawTextureThumbnail(AssetId id, float cellTop)
    {
        var textureId = _controller.GetTextureId(id, out var textureKind);
        if (textureKind != TextureKind.Texture2D)
            return false;
        
        var texPtr = Context.GetTextureRefPtr(textureId);
        GuiLayout.NextAlignTextVerticalTop(cellTop, GuiTheme.ListPaddedRowHeight, GuiTheme.ListRowHeight);
        ImGui.Image(*texPtr.Handle, new Vector2(GuiTheme.ListRowHeight));
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Image(*texPtr.Handle, new Vector2(128, 128));
            ImGui.EndTooltip();
        }

        return true;
    }


    private void TriggerSearch()
    {
        _assetIds.AsSpan(0, _assetCount).Clear();

        var searchStr = _inputUtf8.GetSearchString(out var key, out var mask);

        var search = new SearchPayload<AssetId>(searchStr, _assetIds, key, mask);
        var filter = new SearchAssetFilter(_selectedKind, 0);

        _assetCount = _controller.FilterQuery(in search, filter,
            static (in search, filter, in it) =>
            {
                return SearchQuery(in search, filter, in it);
            });

    }

    private static bool SearchQuery(in SearchPayload<AssetId> search, SearchAssetFilter filter, in AssetQueryItem it)
    {
        if (search.SearchKey > 0 && (it.NameKey & search.SearchMask) != search.SearchKey)
            return false;

        if (search.SearchString.Length > 8 && !it.Name.StartsWith(search.SearchString, StringComparison.Ordinal))
            return false;

        return true;
    }
}