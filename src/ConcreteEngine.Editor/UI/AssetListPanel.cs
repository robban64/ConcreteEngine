using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.EditorConsts;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class AssetListPanel : EditorPanel
{
    private const ImGuiInputTextFlags InputFlags = ImGuiInputTextFlags.CharsNoBlank;
    private const float ListRowHeight = 32f;
    private const float ListPaddedRowHeight = 32f + 6f;

    [FixedAddressValueType] private static SearchStringUtf8 _inputUtf8;

    private readonly NativeViewPtr<byte> _titleStrPtr = TextBuffers.PersistentArena.Alloc(24);

    private readonly AssetId[] _assetIds = new AssetId[AssetCapacity];
    private Vector4 _selectedKindColor = Color4.White;
    private AssetKind _selectedKind;
    private int _assetCount;

    private readonly AssetController _controller;
    private readonly SceneController _sceneController;

    private readonly ComboField _assetCombo;


    public AssetListPanel(StateContext context, AssetController controller, SceneController sceneController) : base(
        PanelId.AssetList, context)
    {
        _controller = controller;
        _sceneController = sceneController;
        _assetCombo = ComboField
            .MakeFromEnumCache<AssetKind>("##asset-combo", () => (int)_selectedKind, OnCategoryChange)
            .WithProperties(FieldGetDelay.VeryHigh, FieldLayout.None)
            .WithPlaceholder("None").WithStartAt(1);
        _assetCombo.Layout = FieldLayout.None;
    }

    public override void Enter()
    {
        if (_assetCount == 0) Search();
    }

    private void OnCategoryChange(Int1Value value)
    {
        var newKind = (AssetKind)value.X;
        if (_selectedKind == newKind) return;

        _selectedKind = newKind;
        _selectedKindColor = StyleMap.GetAssetColor(_selectedKind);

        Search();
    }

    public override void Draw(FrameContext ctx)
    {
        if (_selectedKind == AssetKind.Unknown)
            OnCategoryChange((int)AssetKind.Model);

        DrawHeader();

        if (_assetCount == 0) return;

        ImGui.SeparatorText(_titleStrPtr);

        if (ImGui.BeginTable("asset-list"u8, 3, GuiTheme.TableFlags))
        {
            ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);

            DrawList(ctx);
            ImGui.EndTable();
        }

        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            var payload = ImGui.GetDragDropPayload();
            if (!payload.IsNull && payload.IsDataType("ASSET_MODEL"u8))
            {
                var modelId = *(AssetId*)payload.Data;
                if (!modelId.IsValid()) return;
                var model = _controller.GetAsset<Model>(modelId);
                var camera = EditorCamera.Instance.Camera;
                _sceneController.SpawnSceneObject(model, new Transform(camera.Translation + camera.Forward * 10));
            }
        }
    }

    private void DrawHeader()
    {
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;

        ImGui.SetNextItemWidth(width * 0.62f);
        if (ImGui.InputText("##search-asset"u8, ref _inputUtf8.GetInputRef(), SearchStringUtf8.Length, InputFlags))
            Search();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.38f);
        _assetCombo.Draw();
    }

    private void DrawList(FrameContext ctx)
    {
        var clipper = new ImGuiListClipper();
        clipper.Begin(_assetCount, ListPaddedRowHeight);
        var selectedId = Context.SelectedAssetId;
        while (clipper.Step())
        {
            var idSpan = _assetIds.AsSpan(clipper.DisplayStart, clipper.DisplayEnd - clipper.DisplayStart);
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

        ImGui.TableNextColumn();
        var cellTop = ImGui.GetCursorPosY();

        if (ImGui.Selectable("##select"u8, selected, selectFlags, new Vector2(0, ListRowHeight)))
            Context.EnqueueEvent(new AssetSelectionEvent(id));
        

        var name = _selectedKind switch
        {
            AssetKind.Shader => DrawShaderRow(id, cellTop),
            AssetKind.Model => DrawModelRow(id, cellTop, ctx.Sw),
            AssetKind.Texture => DrawTextureRow(id, cellTop, ctx.Sw),
            AssetKind.Material => DrawMaterialRow(id, cellTop),
            _ => "Unknown"
        };

        ImGui.TableNextColumn();
        GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight);
        ImGui.TextColored(_selectedKindColor, ref ctx.Sw.Append('[').Append(id).Append(']').End());

        ImGui.TableNextColumn();
        GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight);
        ImGui.TextUnformatted(ctx.Sw.Write(name));
    }

    private string DrawTextureRow(AssetId id, float cellTop, UnsafeSpanWriter sw)
    {
        var texture = _controller.GetAsset<Texture>(id);

        if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
        {
            ImGui.SetDragDropPayload("ASSET_TEXTURE"u8, &id, (nuint)Unsafe.SizeOf<AssetId>());

            ImGui.TextUnformatted(sw.Write(texture.Name));

            ImGui.EndDragDropSource();
        }

        if (texture.TextureKind == TextureKind.Texture2D)
        {
            var texPtr = Context.GetTextureRefPtr(texture.GfxId);
            GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight, ListRowHeight * 0.25f);
            ImGui.Image(*texPtr.Handle, new Vector2(ListRowHeight));
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Image(*texPtr.Handle, new Vector2(128, 128));
                ImGui.EndTooltip();
            }
        }
        else
        {
            GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight, GuiTheme.IconSizeMedium);
            AppDraw.DrawIcon(StyleMap.GetIcon(AssetIcons.TextureIcon));
        }

        return texture.Name;
    }

    private string DrawShaderRow(AssetId id, float cellTop)
    {
        var shader = _controller.GetAsset<Shader>(id);

        GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight, GuiTheme.IconSizeMedium);
        AppDraw.DrawIcon(StyleMap.GetIcon(AssetIcons.ShaderIcon));
        return shader.Name;
    }

    private string DrawMaterialRow(AssetId id, float cellTop)
    {
        var material = _controller.GetAsset<Material>(id);

        GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight, GuiTheme.IconSizeMedium);
        AppDraw.DrawIcon(StyleMap.GetIcon(AssetIcons.GetMaterialIcon(material)));
        return material.Name;
    }

    private string DrawModelRow(AssetId id, float cellTop, UnsafeSpanWriter sw)
    {
        var model = _controller.GetAsset<Model>(id);
        if (ImGui.BeginDragDropSource())
        {
            int modelId = model.Id;
            ImGui.SetDragDropPayload("ASSET_MODEL"u8, &modelId, (nuint)Unsafe.SizeOf<AssetId>());
            ImGui.TextUnformatted(sw.Write(model.Name));
            ImGui.EndDragDropSource();
        }

        GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight, GuiTheme.IconSizeMedium);
        AppDraw.DrawIcon(StyleMap.GetIcon(AssetIcons.GetModelIcon(model)));
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

        _titleStrPtr.Writer().Append(_selectedKind.ToText()).Append(" ["u8).Append(_assetCount).Append(']').End();
    }
}