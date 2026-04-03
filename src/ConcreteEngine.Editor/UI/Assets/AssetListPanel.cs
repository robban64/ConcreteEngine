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
using static ConcreteEngine.Editor.Theme.Palette32;

namespace ConcreteEngine.Editor.UI.Assets;

internal sealed unsafe class AssetListPanel : EditorPanel
{
    private const ImGuiInputTextFlags InputFlags = ImGuiInputTextFlags.CharsNoBlank;
    private const float ListRowHeight = 24f;
    private const float ListPaddedRowHeight = 36f;
    private const float ListItemVOffset = (ListPaddedRowHeight - GuiTheme.FontSizeDefault - 1f) * 0.5f;

    // Temp solution
    public static AssetId RenamedAsset;
    
    private static readonly Vector2 ListItemSelectSize = new(0, ListRowHeight);

    private readonly AssetListState _state = new(AssetKind.Texture);
    private readonly AssetBrowser _assetBrowser = new();
    private readonly AssetProvider _provider = EngineObjectStore.AssetProvider;

    private ComboField _assetCombo = null!;

    private NativeViewPtr<byte> _inputStrPtr;

    private readonly Action<int> _onFolderClick;
    private readonly Action<int> _onFileClick;

    public AssetListPanel(StateContext context) : base(PanelId.AssetList, context)
    {
        _onFolderClick = OnFolderClick;
        _onFileClick = OnFileClick;
    }

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
    public override void OnLeave() => _state.BreadcrumbStrPtr.Clear();

    private void Refresh()
    {
        _assetCombo.Refresh();
    }

    public override void OnDraw(FrameContext ctx)
    {
        var state = _state;

        if (state.Sync(RenamedAsset, _assetBrowser))
            Refresh();

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
        var isEmpty = state.TotalDrawCount == 0;
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
            
            var kind = state.SelectedKind;
            var (rootEndIndex, boundEndIndex) = state.Offset;

            start = DrawFolderList((start, int.Min(state.FolderCount, end)), StyleMap.GetIntIcon(Icons.Folder));

            start = DrawFileList((start, rootEndIndex), StyleMap.GetIntIcon(kind.ToIcon()), TextLightBlue);
            start = DrawFileList((start, boundEndIndex), StyleMap.GetIntIcon(kind.ToFileIcon()), TextSecondary);
            DrawFileList((start, end), StyleMap.GetIntIcon(Icons.File), TextMuted);
        }

        clipper.End();
    }

    private int DrawFolderList(RangeU16 range, uint icon)
    {
        var folderPtr = _state.SubFolderPtr;
        for (var i = range.Offset; i < range.ULength; i++)
        {
            ImGui.PushID(-i);
            DrawListRow(i, false, icon, (byte*)(folderPtr + i), _onFolderClick);
            ImGui.PopID();
        }

        return range.ULength;
    }

    private int DrawFileList(RangeU16 range, uint icon, uint color)
    {
        if (range.UOffset >= range.ULength) return range.UOffset;
        
        var folderLength = _state.FolderCount;
        var filePtr = _state.FileItemPtr;
        var indices = _state.GetSearchIndices();

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        for (var i = range.UOffset; i < range.ULength; i++)
        {
            var it = filePtr + indices[i - folderLength];
            ImGui.PushID(it->FileId);
            DrawListRow(i, false, icon, (byte*)&it->Name, _onFileClick);
            ImGui.PopID();
        }
        ImGui.PopStyleColor();
        return range.ULength;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawListRow(int index, bool selected, uint icon, byte* text, Action<int> onSelect)
    {
        const ImGuiSelectableFlags selectFlags = ImGuiSelectableFlags.SpanAllColumns;

        var yOffset = index * ListPaddedRowHeight + ListItemVOffset;
        
        ImGui.TableNextRow(ListRowHeight);
        ImGui.TableNextColumn();
        
        if (ImGui.Selectable("##select"u8, selected, selectFlags, ListItemSelectSize))
            onSelect(index);

        ImGui.SetCursorPosY(yOffset);
        ImGui.TextUnformatted((byte*)&icon);
        ImGui.SameLine();
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
        _state.UpdateTitleText(_assetBrowser);
    }

    
    private void OnFolderClick(int index) => _state.EnqueueDirectory(_assetBrowser.GetChildFolderName(index));

    private void OnFileClick(int index)
    {
        var actualIdx = index - _state.FolderCount;
        if(actualIdx < 0) throw new ArgumentException(nameof(actualIdx));
        
        var rootId = _state.FileItemPtr[actualIdx].AssetRootId;
        if(!rootId.IsValid()) return;
        Context.EnqueueEvent(new SelectionEvent(rootId));
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