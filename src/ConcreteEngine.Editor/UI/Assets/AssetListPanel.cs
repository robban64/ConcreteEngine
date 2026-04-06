using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Theme.Palette32;
using static ConcreteEngine.Editor.Theme.StyleMap;

namespace ConcreteEngine.Editor.UI.Assets;

internal sealed unsafe class AssetListPanel : EditorPanel
{
    private const ImGuiInputTextFlags InputFlags = ImGuiInputTextFlags.CharsNoBlank;
    private const float ListRowHeight = 24f;
    private const float ListPaddedRowHeight = 36f;
    private const float ListItemVOffset = (ListPaddedRowHeight - GuiTheme.FontSizeDefault - 1f) * 0.5f;

    private static AssetProvider Provider => EngineObjectStore.AssetProvider;

    // Temp solution
    public static AssetId RenamedAsset;

    private static readonly Vector2 ListItemSelectSize = new(0, ListRowHeight);

    private readonly AssetListState _state;
    private readonly AssetBrowser _assetBrowser;

    private ComboField _assetCombo = null!;

    private NativeViewPtr<byte> _inputStrPtr = NativeViewPtr<byte>.MakeNull();
    private NativeViewPtr<byte> _breadcrumbStrPtr = NativeViewPtr<byte>.MakeNull();

    private readonly Action<int> _onFolderClick;
    private readonly Action<int> _onFileClick;


    private int TotalDrawCount => _assetBrowser.FolderCount + _state.FilteredCount;

    public AssetListPanel(StateContext context) : base(PanelId.AssetList, context)
    {
        _assetBrowser = new AssetBrowser();
        _state = new AssetListState(_assetBrowser, AssetKind.Texture);

        _onFolderClick = OnFolderClick;
        _onFileClick = OnFileClick;
    }

    public override void OnCreate()
    {
        _assetCombo = ComboField
            .MakeFromEnumCache<AssetKind>("##asset-combo",
                () => _state.PendingKind != 0 ? (int)_state.PendingKind : (int)_assetBrowser.CurrentKind,
                v => _state.EnqueueNewAssetKind((AssetKind)v.X)
            )
            .WithProperties(FieldGetDelay.VeryHigh, FieldLayout.None)
            .WithPlaceholder("None").WithStartAt(1);
        _assetCombo.Layout = FieldLayout.None;


        var builder = CreateAllocBuilder();
        _inputStrPtr = builder.AllocSlice(8);
        _breadcrumbStrPtr = builder.AllocSlice(64);
        _state.NameList = builder.AllocSlice(AssetListState.NameListCapacity);
        PanelMemory = builder.Commit();

        _assetBrowser.BuildFullDirectory();
    }

    private void UpdateTitleText()
    {
        var dirSpan = _assetBrowser.CurrentDirectory.AsSpan();
        var sw = _breadcrumbStrPtr.Writer();
        sw.Append('[').Append(_state.FilteredCount).Append(']').PadRight(2).Append('/');
        foreach (var range in dirSpan.Split('/'))
            sw.Append(dirSpan[range]).Append('/');

        // remove last '/'
        sw.SetCursor(sw.Cursor - 1);
        sw.Append((char)0);
    }


    public override void OnEnter() => Refresh();
    public override void OnLeave() => _breadcrumbStrPtr.Clear();

    private void Refresh()
    {
        _assetCombo.Refresh();
        UpdateTitleText();
        RenamedAsset = default;
    }

    public override void OnDraw(FrameContext ctx)
    {
        var isRootPath = _assetBrowser.IsRootPath;

        if (_state.Sync(RenamedAsset))
            Refresh();

        // Row 1
        if (isRootPath) ImGui.BeginDisabled(true);

        if (ImGui.ArrowButton("prevFolder"u8, ImGuiDir.Left))
            _state.EnqueueDirectory(AssetListState.GoBackString);

        if (isRootPath) ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.SeparatorText(_breadcrumbStrPtr);

        // Row 2
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.62f);
        if (ImGui.InputText("##search-asset"u8, _inputStrPtr, 8, InputFlags))
            OnSearch();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.38f);
        _assetCombo.Draw();

        // List
        if (ImGui.BeginTable("asset-list"u8, 1, GuiTheme.TableFlags))
        {
            ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
            DrawList();
            ImGui.EndTable();

            DragDrop();
        }
    }


    private void DrawList()
    {
        var clipper = new ImGuiListClipper();
        clipper.Begin(TotalDrawCount, ListPaddedRowHeight);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, end = clipper.DisplayEnd;

            var kind = _assetBrowser.CurrentKind;
            var folderCount = _assetBrowser.FolderCount;

            uint folderIcon = GetIntIcon(Icons.Folder),
                assetIcon = GetIntIcon(kind.ToIcon()),
                assetFileIcon = GetIntIcon(kind.ToFileIcon()),
                fileIcon = GetIntIcon(Icons.File);

            start = DrawFolderList(start, int.Min(folderCount, end), folderIcon);

            start = DrawFileList(start, end, folderCount, assetIcon, TextLightBlue);
            start = DrawFileList(start, end, folderCount, assetFileIcon, TextSecondary);
            DrawFileList(start, end, folderCount, fileIcon, TextMuted);
        }

        clipper.End();
    }

    private int DrawFolderList(int start, int end, uint icon)
    {
        var state = _state;
        var onClick = _onFolderClick;
        var indices = state.GetSearchIndices();

        for (var i = start; i < end; i++)
        {
            var name = state.GetFolder(indices[i]);

            ImGui.PushID(-i);
            DrawListRow(name, 0, false, icon, i, i, onClick);
            ImGui.PopID();
        }

        return end;
    }

    private int DrawFileList(int cursor, int end, int offset, uint icon, uint color)
    {
        if (cursor >= end) return cursor;

        ImGui.PushStyleColor(ImGuiCol.Text, color);

        var state = _state;
        var onClick = _onFileClick;
        var indices = state.GetSearchIndices();
        var assetId = int.Min(state.Get(indices[cursor]).AssetRootId, 1);

        for (; cursor < end; cursor++)
        {
            int index = indices[cursor];
            ref readonly var it = ref state.Get(index);
            if (assetId.CompareTo(it.AssetRootId) > 0) break;

            var name = state.GetFolder(index);

            var fileId = it.FileId.Value;
            ImGui.PushID(fileId);
            DrawListRow(name,it.NameLength, false, icon, fileId, cursor, onClick);
            ImGui.PopID();
        }

        ImGui.PopStyleColor();
        return cursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawListRow(byte* text, int textLength, bool selected, uint icon, int id, int index, Action<int> onSelect)
    {
        const ImGuiSelectableFlags selectFlags = ImGuiSelectableFlags.SpanAllColumns;

        var yOffset = index * ListPaddedRowHeight + ListItemVOffset;

        ImGui.TableNextRow(ListRowHeight);
        ImGui.TableNextColumn();

        if (ImGui.Selectable("##select"u8, selected, selectFlags, ListItemSelectSize))
            onSelect(id);

        ImGui.SetCursorPosY(yOffset);
        ImGui.TextUnformatted((byte*)&icon);
        ImGui.SameLine();
        if(textLength > 0)        ImGui.TextUnformatted(text, text + textLength);
        else
        ImGui.TextUnformatted(text);
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
    }


    private void OnFolderClick(int index) => _state.EnqueueDirectory(_assetBrowser.GetChildFolderName(index));

    private void OnFileClick(int id)
    {
        var fileId = new AssetFileId(id);
        if (!fileId.IsValid()) return;
        //var file = _assetBrowser.CurrentNode.FindChild(fileId);
        if (!Provider.TryGetByRootFile(fileId, out var asset)) return;

        Context.EnqueueEvent(new SelectionEvent(asset.Id));
    }


    private void DragDrop()
    {
        if (!ImGui.IsMouseReleased(ImGuiMouseButton.Left)) return;

        var payload = ImGui.GetDragDropPayload();
        if (!payload.IsNull && payload.IsDataType("ASSET_MODEL"u8))
        {
            var modelId = *(AssetId*)payload.Data;
            if (!modelId.IsValid()) return;
            var model = Provider.Get<Model>(modelId);
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
    private string DrawTextureRow(AssetId id, float cellTop, UnsafeSpanWriter sw)
    {
        var texture = Provider.Get<Texture>(id);

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
            AppDraw.DrawIcon(GetIcon(AssetIcons.TextureIcon));
        }

        return texture.Name;
    }

    private string DrawShaderRow(AssetId id, float cellTop)
    {
        var shader = Provider.Get<Shader>(id);

        GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight, GuiTheme.IconSizeMedium);
        AppDraw.DrawIcon(GetIcon(AssetIcons.ShaderIcon));
        return shader.Name;
    }

    private string DrawMaterialRow(AssetId id, float cellTop)
    {
        var material = Provider.Get<Material>(id);

        GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight, GuiTheme.IconSizeMedium);
        AppDraw.DrawIcon(GetIcon(AssetIcons.GetMaterialIcon(material)));
        return material.Name;
    }

    private string DrawModelRow(AssetId id, float cellTop, UnsafeSpanWriter sw)
    {
        var model = Provider.Get<Model>(id);
        if (ImGui.BeginDragDropSource())
        {
            int modelId = model.Id;
            ImGui.SetDragDropPayload("ASSET_MODEL"u8, &modelId, (nuint)Unsafe.SizeOf<AssetId>());
            ImGui.TextUnformatted(sw.Write(model.Name));
            ImGui.EndDragDropSource();
        }

        GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight, GuiTheme.IconSizeMedium);
        AppDraw.DrawIcon(GetIcon(AssetIcons.GetModelIcon(model)));
        return model.Name;
    }
}