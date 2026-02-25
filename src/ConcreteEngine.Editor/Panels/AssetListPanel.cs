using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.EditorConsts;

namespace ConcreteEngine.Editor.Panels;

internal sealed unsafe class AssetListPanel : EditorPanel
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

    public override void Draw(FrameContext ctx)
    {
        DrawHeader();

        if (_selectedKind == AssetKind.Unknown || _assetCount == 0) return;

        ImGui.SeparatorText(ref ctx.Sw.Append(_selectedKind.ToText()).Append(" ["u8).Append(_assetCount).Append(']')
            .End());
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
    }

    private void DrawAssetList(FrameContext ctx)
    {
        var clipper = new ImGuiListClipper();
        clipper.Begin(_assetCount, GuiTheme.ListPaddedRowHeight);
        var selectedId = Context.SelectedAssetId;
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, length = clipper.DisplayEnd - start;
            var idSpan = _assetIds.AsSpan(start, length);
            foreach (var id in idSpan)
            {
                ImGui.PushID(id);
                var selected = id == selectedId;
                DrawTableRow(id, selected, ctx);
                ImGui.PopID();
            }
        }

        clipper.End();
    }

    private void DrawTableRow(AssetId id, bool selected, FrameContext ctx)
    {
        const ImGuiSelectableFlags
            selectFlags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        ImGui.TableNextRow();
        var cellTop = ImGui.GetCursorPosY();

        ImGui.TableNextColumn();
        if (ImGui.Selectable("##select"u8, selected, selectFlags, new Vector2(0, GuiTheme.ListRowHeight)))
            Context.EnqueueEvent(new AssetSelectionEvent(id));

        var name = _selectedKind switch
        {
            AssetKind.Shader => DrawShaderRow(id, cellTop, ctx),
            AssetKind.Model => DrawModelRow(id, cellTop, ctx),
            AssetKind.Texture => DrawTextureRow(id, cellTop, ctx),
            AssetKind.Material => DrawMaterialRow(id, cellTop, ctx),
            _ => throw new ArgumentOutOfRangeException()
        };

        ImGui.TableNextColumn();
        GuiLayout.NextAlignTextVerticalTop(cellTop, GuiTheme.ListRowHeight);
        ImGui.TextColored(_selectedKindColor, ref ctx.Sw.Append('[').Append(id).Append(']').End());

        ImGui.TableNextColumn();
        GuiLayout.NextAlignTextVerticalTop(cellTop, GuiTheme.ListRowHeight);
        ImGui.TextUnformatted(ctx.Write(name));
    }

    private string DrawTextureRow(AssetId id, float cellTop, FrameContext ctx)
    {
        var texture = _controller.GetAsset<Texture>(id);

        if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
        {
            ImGui.SetDragDropPayload("ASSET_TEXTURE"u8, &id, (nuint)Unsafe.SizeOf<AssetId>());

            ImGui.TextUnformatted(ctx.Write(texture.Name));

            ImGui.EndDragDropSource();
        }

        if (texture.TextureKind == TextureKind.Texture2D)
        {
            var texPtr = Context.GetTextureRefPtr(texture.GfxId);
            GuiLayout.NextAlignTextVerticalTop(cellTop, GuiTheme.ListPaddedRowHeight, GuiTheme.ListRowHeight);
            ImGui.Image(*texPtr.Handle, new Vector2(GuiTheme.ListRowHeight));
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Image(*texPtr.Handle, new Vector2(128, 128));
                ImGui.EndTooltip();
            }
        }
        else
        {
            GuiLayout.NextAlignTextVerticalTop(cellTop, GuiTheme.ListRowHeight, GuiTheme.IconMediumSize);
            AppDraw.DrawIcon(ctx.Write(AssetIcons.GetTextureIcon()));
        }

        return texture.Name;
    }

    private string DrawShaderRow(AssetId id, float cellTop, FrameContext ctx)
    {
        var shader = _controller.GetAsset<Shader>(id);

        GuiLayout.NextAlignTextVerticalTop(cellTop, GuiTheme.ListRowHeight, GuiTheme.IconMediumSize);
        AppDraw.DrawIcon(ctx.Write(AssetIcons.GetShaderIcon()));
        return shader.Name;
    }

    private string DrawMaterialRow(AssetId id, float cellTop, FrameContext ctx)
    {
        var material = _controller.GetAsset<Material>(id);

        GuiLayout.NextAlignTextVerticalTop(cellTop, GuiTheme.ListRowHeight, GuiTheme.IconMediumSize);
        AppDraw.DrawIcon(ctx.Write(AssetIcons.GetMaterialIcon(material)));
        return material.Name;
    }

    private string DrawModelRow(AssetId id, float cellTop, FrameContext ctx)
    {
        var model = _controller.GetAsset<Model>(id);

        GuiLayout.NextAlignTextVerticalTop(cellTop, GuiTheme.ListRowHeight, GuiTheme.IconMediumSize);
        AppDraw.DrawIcon(ctx.Write(AssetIcons.GetModelIcon(model)));
        return model.Name;
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