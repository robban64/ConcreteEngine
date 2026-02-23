using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.EditorConsts;

namespace ConcreteEngine.Editor.Panels;

internal sealed class AssetListPanel : EditorPanel
{
    private static SearchStringUtf8 _inputUtf8;

    private readonly ComboField _assetCombo;

    private readonly AssetId[] _assetIds = new AssetId[AssetCapacity];
    private int _assetCount;

    private AssetKind _selectedKind;
    private Color4 _selectedKindColor = Color4.White;
    private readonly AssetController _controller;

    public AssetListPanel(PanelContext context, AssetController controller) : base(PanelId.AssetList, context)
    {
        _controller = controller;
        _assetCombo = ComboField.MakeFromEnumCache<AssetKind>("##asset-combo", "None", null, OnCategoryChange);
    }

    public override void Enter()
    {
        if (_assetCount == 0) Search();
    }

    private void OnCategoryChange(int value)
    {
        var newKind = (AssetKind)value;
        if (_selectedKind == newKind) return;

        _selectedKind = newKind;
        _selectedKindColor = StyleMap.GetAssetColor(_selectedKind);

        Search();
    }

    public override void Draw(in FrameContext ctx)
    {
        DrawHeader();

        if (_selectedKind == AssetKind.Unknown || _assetCount == 0) return;

        var sw = ctx.Sw;
        ImGui.SeparatorText(ref sw.Start(_selectedKind.ToText()).Append(" ["u8).Append(_assetCount).Append(']').End());
        if (ImGui.BeginTable("asset-list"u8, 3, GuiTheme.TableFlags))
        {
            ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);

            DrawAssetList(ctx);

            ImGui.EndTable();
        }
    }

    private void DrawHeader()
    {
        const ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.CharsNoBlank;
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;

        ImGui.SetNextItemWidth(width * 0.62f);
        if (ImGui.InputText("##search-asset"u8, ref _inputUtf8.GetInputRef(), SearchStringUtf8.Length, inputFlags))
            Search();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.38f);
        _assetCombo.DrawComponent();

        /*
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
            TriggerSearch();
            */
    }

    private void DrawAssetList(FrameContext ctx)
    {
        var clipper = new ImGuiListClipper();
        clipper.Begin(_assetCount, GuiTheme.ListPaddedRowHeight);
        var selectedId = Context.SelectedAssetId;
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, length = clipper.DisplayEnd - start;
            var span = _assetIds.AsSpan(start, length);
            foreach (var id in span)
            {
                ImGui.PushID(id);
                DrawTableRow(id, selectedId, ctx);
                ImGui.PopID();
            }
        }

        clipper.End();
    }

    private void DrawTableRow(AssetId id, AssetId selectedId, FrameContext ctx)
    {
        const ImGuiSelectableFlags
            selectFlags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        var selected = id == selectedId;
        var name = _controller.GetAsset(id).Name;
        var isTexture = _selectedKind == AssetKind.Texture;

        ImGui.TableNextRow();
        var cellTop = ImGui.GetCursorPosY();

        ImGui.TableNextColumn();
        if (ImGui.Selectable("##select"u8, selected, selectFlags, new Vector2(0, GuiTheme.ListRowHeight)))
            Context.EnqueueEvent(new AssetEvent(id));

        if (isTexture && ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
        {
            unsafe { ImGui.SetDragDropPayload("ASSET_TEXTURE"u8, &id, sizeof(int)); }

            ImGui.TextUnformatted(ref ctx.Sw.Write(name));

            ImGui.EndDragDropSource();
        }

        if (!isTexture || !DrawTextureThumbnail(id, cellTop))
        {
            GuiLayout.NextAlignTextVerticalTop(cellTop, GuiTheme.ListRowHeight, GuiTheme.IconMediumSize);
            AppDraw.DrawIcon(ref _selectedKind.GetIcon().GetRef());
        }

        ImGui.TableNextColumn();
        GuiLayout.NextAlignTextVerticalTop(cellTop, GuiTheme.ListRowHeight);
        ImGui.TextColored(_selectedKindColor, ref ctx.Sw.Start('[').Append(id).Append(']').End());

        ImGui.TableNextColumn();
        GuiLayout.NextAlignTextVerticalTop(cellTop, GuiTheme.ListRowHeight);

        ImGui.TextUnformatted(ref ctx.Sw.Write(name));
    }


    private unsafe bool DrawTextureThumbnail(AssetId id, float cellTop)
    {
        var texture = _controller.GetAsset<Texture>(id);
        if (texture.TextureKind != TextureKind.Texture2D) return false;

        var texPtr = Context.GetTextureRefPtr(texture.GfxId);
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
    
    private void Search()
    {
        if (_selectedKind == AssetKind.Unknown) return;
        _assetIds.AsSpan(0, _assetCount).Clear();

        var searchString = _inputUtf8.GetSearchString(out var searchKey, out var searchMask);
        if (!int.TryParse(searchString, out var searchId)) searchId = 0;

        var count = 0;
        var assets = _controller.GetAssetSpan(_selectedKind);
        foreach (var it in assets)
        {
            if (count >= AssetCapacity) break;

            if (searchKey <= 0 || searchId == it.Id || (it.PackedName & searchMask) == searchKey)
                _assetIds[count++] = it.Id;
        }

        _assetCount = count;
    }

}