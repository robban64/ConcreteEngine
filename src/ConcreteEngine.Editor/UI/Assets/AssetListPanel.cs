using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Assets;


internal sealed unsafe class AssetListPanel(StateContext context) : EditorPanel(PanelId.AssetList, context)
{
    private const ImGuiInputTextFlags InputFlags = ImGuiInputTextFlags.CharsNoBlank;
    private const float ListRowHeight = 24f;
    private const float ListPaddedRowHeight = 24f + 6f;

    private readonly AssetListState _state = new(AssetKind.Texture);
    private readonly AssetBrowser _assetBrowser = new(EngineObjectStore.AssetProvider);
    private readonly AssetProvider _provider = EngineObjectStore.AssetProvider;

    private ComboField _assetCombo = null!;
    
    private NativeViewPtr<byte> _inputStrPtr;

    public override void OnCreate()
    {
        _assetCombo = ComboField
            .MakeFromEnumCache<AssetKind>("##asset-combo",
                () => _state.PendingKind != 0 ? (int)_state.PendingKind : (int)_state.SelectedKind,
                v => _state.EnqueueNewAssetKind((AssetKind)v.X)
            )
            .WithProperties(FieldGetDelay.VeryHigh, FieldLayout.None)
            .WithPlaceholder("None").WithStartAt(1);
        _assetCombo.Layout = FieldLayout.None;


        var builder = CreateAllocBuilder();
        _inputStrPtr = builder.AllocSlice(8);
        _state.BreadcrumbStrPtr = builder.AllocSlice(64);
        PanelMemory = builder.Commit();

        _assetBrowser.BuildFullDirectory();
    }

    public override void OnEnter() => Refresh();

    private void Refresh()
    {
        _assetCombo.Refresh();
    }

    public override void OnDraw(FrameContext ctx)
    {
        if (_state.SyncStateToBrowser(_assetBrowser))
            Refresh();

        DrawHeader();

        bool isEmpty = _assetBrowser.TotalCount == 0;
        if (isEmpty || !ImGui.BeginTable("asset-list"u8, 2, GuiTheme.TableFlags)) return;

        ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed);
        //ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);

        DrawList(ctx.Sw);
        ImGui.EndTable();

        DragDrop();
    }

    private void DrawHeader()
    {
        var state = _state;

        // Row 1
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.62f);
        if (ImGui.InputText("##search-asset"u8, _inputStrPtr, 8, InputFlags))
            OnSearch();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.38f);
        _assetCombo.Draw();

        // Row 2
        if (state.IsRootPath) ImGui.BeginDisabled(true);

        if (ImGui.ArrowButton("prevFolder"u8, ImGuiDir.Left))
            state.EnqueueDirectory(AssetListState.GoBackString);

        if (state.IsRootPath) ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.SeparatorText(state.BreadcrumbStrPtr);
    }

    private void DrawList(UnsafeSpanWriter sw)
    {
        var assetBrowser = _assetBrowser;
        if (assetBrowser.TotalCount == 0) return;

        var clipper = new ImGuiListClipper();
        clipper.Begin(assetBrowser.FolderCount + assetBrowser.FilteredCount, ListPaddedRowHeight);
        while (clipper.Step())
        {
            var folders = assetBrowser.GetSubFolders();
            int start = clipper.DisplayStart, end = clipper.DisplayEnd, folderLength = folders.Length;
            for (int i = start; i < folderLength; i++)
            {
                ImGui.PushID(-i);
                DrawFolderRow(folders[i], sw);
                ImGui.PopID();
            }

            var entries = assetBrowser.GetEntries();
            var indices = new UnsafeSpan<int>(assetBrowser.GetSearchIndices());
            for (int i = start + folderLength; i < end; i++)
            {
                var it = entries[indices[i - folderLength]];
                ImGui.PushID(it.FileId);
                DrawFileRow(it, sw);
                ImGui.PopID();
            }
        }

        clipper.End();
    }

    private void DrawFolderRow(string name, UnsafeSpanWriter sw)
    {
        const ImGuiSelectableFlags selectFlags = ImGuiSelectableFlags.SpanAllColumns;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        var cellTop = ImGui.GetCursorPosY();
        if (ImGui.Selectable("##select"u8, false, selectFlags, new Vector2(0, ListRowHeight)))
            _state.EnqueueDirectory(name);

        GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight);
        ImGui.TextUnformatted(StyleMap.GetIcon(Icons.Folder));

        AppDraw.ColumnVTop(sw.Write(name), cellTop, ListRowHeight);
    }

    private void DrawFileRow(AssetFileDisplayItem it, UnsafeSpanWriter sw)
    {
        const ImGuiSelectableFlags
            selectFlags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        var selected = it.FileId == _state.SelectedFileId;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        var cellTop = ImGui.GetCursorPosY();
        if (ImGui.Selectable("##select"u8, selected, selectFlags, new Vector2(0, ListRowHeight)))
        {
            var asset = it.IsAssetRootFile ? _provider.GetAsset(it.AssetRootId) : null;
            if (asset != null)
                Context.EnqueueEvent(new SelectionEvent(it.AssetRootId));
        }

        GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight);

        ImGui.TextColored(it.IsAssetRootFile ? _state.CurrentColor : Palette.TextMuted,
            StyleMap.GetIcon(_state.SelectedKind.ToIcon()));

        AppDraw.ColumnVTop(sw.Write(it.Name), cellTop, ListRowHeight);
    }

    private void OnSearch()
    {
        if (_inputStrPtr[0] == 0)
        {
            _assetBrowser.SetSearch(0, 0);
            return;
        }

        Span<char> chars = stackalloc char[_inputStrPtr.Length];
        InputTextUtils.GetSearchString(_inputStrPtr.AsSpan(), chars, out var searchKey, out var searchMask);
        if (searchKey == 0) return;
        _assetBrowser.SetSearch(searchKey, searchMask);
        _state.UpdateTitleText(_assetBrowser);
    }

    private void DragDrop()
    {
        if (!ImGui.IsMouseReleased(ImGuiMouseButton.Left)) return;

        var payload = ImGui.GetDragDropPayload();
        if (!payload.IsNull && payload.IsDataType("ASSET_MODEL"u8))
        {
            var modelId = *(AssetId*)payload.Data;
            if (!modelId.IsValid()) return;
            var model = _provider.GetAsset<Model>(modelId);
            var camera = EditorCamera.Instance.Camera;
            var transform = new Transform(camera.Translation + camera.Forward * 10);
            EngineObjectStore.SceneController.SpawnSceneObject(model, transform);
        }
    }
    /*
        var name = _selectedKind switch
        {
            AssetKind.Shader => DrawShaderRow(id, cellTop),
            AssetKind.Model => DrawModelRow(id, cellTop, sw),
            AssetKind.Texture => DrawTextureRow(id, cellTop, sw),
            AssetKind.Material => DrawMaterialRow(id, cellTop),
            _ => "Unknown"
        };

        ImGui.TableNextColumn();
        GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight);
        ImGui.TextColored(_selectedKindColor, sw.Append('[').Append(it.FileId).Append(']').End());

        ImGui.TableNextColumn();
        GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight);
        ImGui.TextUnformatted(sw.Write(it.Name));
*/

    /*
        Span<char> chars = stackalloc char[_inputStrPtr.Length];
        chars = InputTextUtils.GetSearchString(_inputStrPtr.AsSpan(), chars, out var searchKey, out var searchMask);
        if (!int.TryParse(chars, out var searchId)) searchId = 0;
        var count = 0;
        foreach (var it in _provider.AssetEnumerator(_selectedKind))
        {
            if (count >= AssetCapacity) break;

            if (searchKey <= 0 || searchId == it.Id || (it.PackedName & searchMask) == searchKey)
                _assetIds[count++] = it.Id;
        }


        _titleStrPtr.Writer().Append(_selectedKind.ToText()).Append(" ["u8).Append(_assetCount).Append(']').End();
*/

    private string DrawTextureRow(AssetId id, float cellTop, UnsafeSpanWriter sw)
    {
        var texture = _provider.GetAsset<Texture>(id);

        if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
        {
            ImGui.SetDragDropPayload("ASSET_TEXTURE"u8, &id, (nuint)Unsafe.SizeOf<AssetId>());

            ImGui.TextUnformatted(sw.Write(texture.Name));

            ImGui.EndDragDropSource();
        }

        if (texture.TextureKind == TextureKind.Texture2D && Context.TryGetTextureRefPtr(texture.GfxId, out var texPtr))
        {
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
        var shader = _provider.GetAsset<Shader>(id);

        GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight, GuiTheme.IconSizeMedium);
        AppDraw.DrawIcon(StyleMap.GetIcon(AssetIcons.ShaderIcon));
        return shader.Name;
    }

    private string DrawMaterialRow(AssetId id, float cellTop)
    {
        var material = _provider.GetAsset<Material>(id);

        GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight, GuiTheme.IconSizeMedium);
        AppDraw.DrawIcon(StyleMap.GetIcon(AssetIcons.GetMaterialIcon(material)));
        return material.Name;
    }

    private string DrawModelRow(AssetId id, float cellTop, UnsafeSpanWriter sw)
    {
        var model = _provider.GetAsset<Model>(id);
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
}