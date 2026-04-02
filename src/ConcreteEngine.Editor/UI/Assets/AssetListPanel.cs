using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
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
    private const float ListItemVOffset = (ListRowHeight - GuiTheme.FontSizeDefault) * 0.5f;

    private static readonly Vector2 ListItemSelectSize = new(0, ListRowHeight);

    private readonly AssetListState _state = new(AssetKind.Texture);
    private readonly AssetBrowser _assetBrowser = new();
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
        _state.ListBufferPtr = builder.AllocSlice(AssetListState.Capacity);
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

        var state = _state;

        // Row 1
        if (state.IsRootPath) ImGui.BeginDisabled(true);

        if (ImGui.ArrowButton("prevFolder"u8, ImGuiDir.Left))
            state.EnqueueDirectory(AssetListState.GoBackString);

        if (state.IsRootPath) ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.SeparatorText(state.BreadcrumbStrPtr);

        // Row 2
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.62f);
        if (ImGui.InputText("##search-asset"u8, _inputStrPtr, 8, InputFlags))
            OnSearch();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.38f);
        _assetCombo.Draw();


        // List
        bool isEmpty = _state.TotalDrawCount == 0;
        if (isEmpty || !ImGui.BeginTable("asset-list"u8, 1, GuiTheme.TableFlags)) return;

        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        DrawList();
        ImGui.EndTable();

        DragDrop();
    }


    private void DrawList()
    {
        var state = _state;
        var clipper = new ImGuiListClipper();
        clipper.Begin(state.TotalDrawCount, ListPaddedRowHeight);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, end = clipper.DisplayEnd;
            var offset = state.Offset;

            var i = DrawFolderList(state, start, int.Min(state.FolderCount, end));

            i = DrawFileList(i, offset.RootEndIndex, state.SelectedKind.ToIcon(), Palette.TextLightBlue, state);
            ImGui.PopStyleColor();

            i = DrawFileList(i, offset.BoundEndIndex, state.SelectedKind.ToFileIcon(), Palette.TextSecondary, state);
            ImGui.PopStyleColor();

            DrawFileList(i, end, Icons.File, Palette.TextMuted, state);
            ImGui.PopStyleColor();
        }

        clipper.End();
    }

    private int DrawFolderList(AssetListState state, int start, int end)
    {
        var folders = state.GetSubFolders();
        for (int i = start; i < end; i++)
        {
            ImGui.PushID(-i);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawFolderRow(i, (byte*)(folders + i));
            ImGui.PopID();
        }

        return end;
    }

    private int DrawFileList(int i, int end, Icons icon, Vector4 color, AssetListState state)
    {
        var entries = state.GetEntries();
        var indices = state.GetSearchIndices();
        var folderLength = state.FolderCount;
        var selectedFileId = state.SelectedFileId;

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ref byte iconRef = ref *StyleMap.GetIcon(icon);
        for (; i < end; i++)
        {
            ref var it = ref *(entries + indices[i - folderLength]);
            ImGui.PushID(it.FileId);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawFileRow(ref it, ref iconRef, selectedFileId);
            ImGui.PopID();
        }

        return i;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawFolderRow(int index, byte* name)
    {
        const ImGuiSelectableFlags selectFlags = ImGuiSelectableFlags.SpanAllColumns;

        var yOffset = ImGui.GetCursorPosY() + ListItemVOffset;
        if (ImGui.Selectable("##select"u8, false, selectFlags, ListItemSelectSize))
            _state.EnqueueDirectory(_assetBrowser.GetChildFolderName(index));

        ImGui.SetCursorPosY(yOffset);
        ImGui.TextUnformatted(StyleMap.GetIcon(Icons.Folder));
        ImGui.SameLine();
        ImGui.TextUnformatted(name);
    }
    /*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawFolderRow(int index, byte* name)
    {
        const ImGuiSelectableFlags selectFlags = ImGuiSelectableFlags.SpanAllColumns;

        var yOffset = ImGui.GetCursorPosY() + ListItemVOffset;
        var ui = UiCursor.Make();
        ui.Pos.Y += ListItemVOffset;
        if (ImGui.Selectable("##select"u8, false, selectFlags, ListItemSelectSize))
            _state.EnqueueDirectory(_assetBrowser.GetChildFolderName(index));

        ui.Text(ref *StyleMap.GetIcon(Icons.Folder));
        ui.SameLine();
        ui.Text(ref * name);
    }
*/
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawFileRow(ref AssetFileDisplayItem it, ref byte icon, AssetFileId selectedFileId)
    {
        const ImGuiSelectableFlags
            selectFlags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        var selected = it.FileId == selectedFileId;
        var yOffset = ImGui.GetCursorPosY() + ListItemVOffset;
        if (ImGui.Selectable("##select"u8, selected, selectFlags, ListItemSelectSize))
        {
            var asset = it.IsAssetRootFile ? _provider.GetAsset(it.AssetRootId) : null;
            if (asset != null)
                Context.EnqueueEvent(new SelectionEvent(it.AssetRootId));
        }

        ImGui.SetCursorPosY(yOffset);
        ImGui.TextUnformatted(ref icon);
        ImGui.SameLine();
        ImGui.TextUnformatted(ref it.Name.GetRef());
    }

    private void OnSearch()
    {
        if (_inputStrPtr[0] == 0)
        {
            _state.SetSearch(0, 0);
            return;
        }

        Span<char> chars = stackalloc char[_inputStrPtr.Length];
        var str = InputTextUtils.GetSearchString(_inputStrPtr.AsSpan(), chars, out var searchKey, out var searchMask);
        if (searchKey == 0 || str.IsEmpty) return;
        _state.SetSearch(searchKey, searchMask);
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