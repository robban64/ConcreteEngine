using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Widgets;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Theme.Palette32;
using static ConcreteEngine.Editor.Theme.StyleMap;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class AssetsWindow : EditorWindow
{
    private const float ListItemHeight = 24f;
    private static float ListItemPad => GuiTheme.CellPadding.X * 2f;

    private readonly AssetBrowser _assetBrowser;

    private readonly TextInput _searchInput;
    private readonly ComboInput _assetCombo;

    private AssetFileId _selectedFile;
    private AssetId _selectedAssetId;

    private RangeU16 _breadcrumbStrHandle;

    private MemoryBlockPtr _memory;

    public AssetsWindow(StateManager state) : base(state)
    {
        _assetBrowser = new AssetBrowser(OnDirectoryChange);

        _searchInput = new TextInput("search", 8)
            .WithFilter(TextInputFilter.None, allowEmpty: true)
            .WithTransformer(trimmed: true, lowercase: true)
            .WithCallbackU16((searchString) => _assetBrowser.SetSearch(searchString));

        _assetCombo = ComboInput.MakeFromEnumCache<AssetKind>("asset-combo");
    }

    public void SelectAsset(AssetId id)
    {
        _selectedFile = id.IsValid() ? AssetManager.Instance.GetAssetRootFile(id).Id : AssetFileId.Empty;
        _selectedAssetId = id;
    }

    public override ReadOnlySpan<byte> Id => WindowRoot.AssetWindowId;

    protected override void OnCreate()
    {
        var allocator = TextBuffers.PersistentArena.MakeBuilder();
        var inputHandle = allocator.AllocSlice(8).AsRange16();
        _breadcrumbStrHandle = allocator.AllocSlice(64).AsRange16();
        _memory = TextBuffers.PersistentArena.CommitBuilder(allocator);

        _searchInput.SetTextBuffer(_memory.SliceData(inputHandle));

        //_state.Memory = TextBuffers.PersistentArena.Alloc(AssetListState.NameListCapacity);
        _assetBrowser.BuildFullDirectory();
    }

    private void Sync()
    {
        if (State.Context.Selection.SelectedAssetId != _selectedAssetId)
            SelectAsset(State.Context.Selection.SelectedAssetId);
    }


    private void OnDirectoryChange(AssetBrowser browser)
    {
        UpdateTitleText();

        var searchText = _searchInput.GetTextSpan();
        if (searchText.Length == 0)
        {
            browser.SetSearch(ReadOnlySpan<char>.Empty);
            return;
        }

        Span<char> searchTextU16 = stackalloc char[Encoding.UTF8.GetCharCount(searchText)];
        Encoding.UTF8.GetChars(searchText, searchTextU16);
        browser.SetSearch(searchTextU16);

    }

    private void UpdateTitleText()
    {
        var path = _assetBrowser.CurrentNode.GetRelativePath();
        var sw = _memory.Data.Slice(_breadcrumbStrHandle).Writer();
        if (path.Length == 0)
        {
            sw.Write('/');
            return;
        }

        //sw.Append('[').Append().Append(']').PadRight(2);
        foreach (var range in path.Split('/'))
            sw.Append(path[range]).Append('/');

        sw.SetCursor(sw.Cursor - 1); // remove last '/'
        sw.End();
    }


    protected override void OnDraw()
    {
        Sync();

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2,2));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 2));
/*
        if (ImGui.BeginChild("folders"u8, new Vector2(0, 32)))
        {
            ImGui.SetNextItemWidth(100);
            _searchInput.Draw();
            
            ImGui.SameLine();

            ImGui.SetNextItemWidth(80);
            if (_assetCombo.Draw()) ;
        }
        ImGui.EndChild();
        */
        ImGui.BeginMenuBar();
        
        ImGui.EndMenuBar();

        if (ImGui.BeginChild("folders"u8, new Vector2(200, 0), ImGuiChildFlags.Borders))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0, 0.5f));
            DrawFolders();
            ImGui.PopStyleVar();
        }
        ImGui.EndChild();

        ImGui.SameLine();

        if (ImGui.BeginChild("files"u8, Vector2.Zero, ImGuiChildFlags.Borders))
        {
            var isRootPath = _assetBrowser.IsRootPath;
            if (isRootPath) ImGui.BeginDisabled(true);
        
            if (ImGui.Button(GetIcon(Icons.ChevronLeft))) 
                _assetBrowser.GoToParent();

            if (isRootPath) ImGui.EndDisabled(); 
        
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(_memory.Data.Slice(_breadcrumbStrHandle));

            ImGui.Separator();
            
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            DrawFiles();
            ImGui.PopStyleVar();
        }
        ImGui.EndChild();
        ImGui.PopStyleVar(2);

    }
    

    private const float GridPadding = 16.0f;
    private const float GridInnerSize = 80.0f;
    private const float GridCellSize = GridInnerSize + GridPadding;
    private const float GridIconSize = GuiTheme.IconSizeLarge;

    private static readonly Vector2 ItemSize = new(GridInnerSize);

    private static readonly Vector2 IconBasePos = new((GridInnerSize - GridIconSize) * 0.5f,
        (GridInnerSize * 0.4f) - (GridIconSize * 0.5f));

    private void DrawFolders()
    {
        var sw = TextBuffers.GetWriter();
        var icon = GetIntIcon(Icons.Folder);
        var nodes = _assetBrowser.CurrentNode.GetChildren();
        for (var i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            var previewName = node.PreviewName;

            var text = sw
                .AppendIcon((byte*)&icon).PadRight(2)
                .Append((byte*)&previewName).AppendImGuiId(i)
                .End();

            if (ImGui.Selectable(text, false, 0, new Vector2(0, ListItemHeight)))
                _assetBrowser.GoToChild(node.GetFolderName());
        }
    }

    private void DrawFiles()
    {
        if (_assetBrowser.FilteredCount == 0 || _assetBrowser.FileCount == 0) return;
        
        var count = _assetBrowser.FilteredCount;

        int columnCount = (int)(ImGui.GetContentRegionAvail().X / GridCellSize);
        columnCount = int.Max(columnCount, 1);
        var rowCount = (int)float.Ceiling(count / (float)columnCount);

        ImGuiListClipper clipper = default;
        clipper.Begin(rowCount, GridCellSize);
        while (clipper.Step())
        {
            var start = clipper.DisplayStart * columnCount;
            var length = (clipper.DisplayEnd * columnCount) - start;
            length = int.Min(length, count);
            DrawFilesInner(start, length, columnCount, _selectedFile);
        }

        clipper.End();
    }

    private void DrawFilesInner(int start, int length, int columnCount, AssetFileId selectedFileId)
    {
        var sw = TextBuffers.GetWriter();
        var drawList = ImGui.GetWindowDrawList();
        var span = _assetBrowser.GetFileItems(start, length);
        for (var index = 0; index < span.Length; index++)
        {
            var file = span[index];

            AssetsExtensions.GetIconAndColor(file.Binding, AssetKind.Texture, out var icon, out var color);

            var startPos = ImGui.GetCursorScreenPos(); // top left

            if (ImGui.Selectable(sw.AppendImGuiId(file.Id).End(), selectedFileId == file.Id, 0, ItemSize))
                OnListItemClick(file.Id);

            drawList.PushClipRect(startPos, startPos + ItemSize, true);

            GuiTheme.PushFontIconLarge();
            drawList.AddText(IconBasePos + startPos, color, (byte*)&icon);
            ImGui.PopFont();

            var textBegin = (byte*)&file.DisplayName;
            var textEnd = textBegin + file.DisplayName.Count;

            var labelSize = ImGui.CalcTextSize(textBegin, textEnd);
            var labelPos = new Vector2(
                startPos.X + (GridInnerSize - float.Min(labelSize.X, GridInnerSize)) * 0.5f,
                startPos.Y + (GridInnerSize * 0.8f) - (labelSize.Y * 0.5f)
            );

            drawList.AddText(labelPos, TextPrimary, textBegin, textEnd);
            drawList.PopClipRect();

            int nextGridIndex = (index + 1) % columnCount;
            if (nextGridIndex != 0 && index + 1 < length)
            {
                ImGui.SameLine(0.0f, GridPadding);
            }
            else
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + GridPadding);
                ImGui.Dummy(default);
            }
        }
    }

    private void OnListItemClick(AssetFileId fileId)
    {
        if (!fileId.IsValid() || !AssetManager.FileRegistry.TryGetFile(fileId, out var file) ||
            !file.AssetRootId.IsValid())
        {
            return;
        }

        State.EnqueueEvent(new SelectionEvent(file.AssetRootId));
    }
}

/*
    private void DrawList()
    {
        var selectedFileId = _selectedFile;
        var currentKind = _assetBrowser.CurrentKind;

        ImGuiListClipper clipper = default;
        clipper.Begin(TotalDrawCount, ListItemHeight + ListItemPad);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, end = clipper.DisplayEnd;
            for (var i = 0; i < 4; i++)
            {
                var (icon, color) = GetIconAndColor((FileBinding)i, currentKind);
                ImGui.PushStyleColor(ImGuiCol.Text, color);
                start = DrawList(start, end, icon, selectedFileId, currentKind, (FileBinding)i);
                ImGui.PopStyleColor();
            }
        }

        clipper.End();
    }

    private int DrawList(int start, int end, uint icon, AssetFileId selectedId, AssetKind kind, FileBinding binding)
    {
        const ImGuiSelectableFlags selectFlags = ImGuiSelectableFlags.AllowDoubleClick;

        if ((uint)start >= (uint)end) return start;

        var writer = TextBuffers.GetWriter();
        var indices = _state.GetSearchIndices();

        for (var i = start; i < end; i++)
        {
            var name = _state.GetDrawData(indices[i], out var it);
            if (it.Binding != binding) return i;

            var selected = selectedId.Id > 0 && it.FileId == selectedId;
            ImGui.PushID(binding == FileBinding.Unknown ? -it.FolderIndex : it.FileId.Id);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            var text = writer.AppendIcon((byte*)&icon).PadRight(2).Append(name).End();
            if (ImGui.Selectable(text, selected, selectFlags, ListItemSelectSize))
                OnListItemClick(it);

            if (kind == AssetKind.Model && binding == FileBinding.RootFile && ImGui.BeginDragDropSource())
            {
                AssetManager.FileRegistry.TryGetByRootFileId(it.FileId, out var modelId);
                ImGui.SetDragDropPayload("ASSET_MODEL"u8, &modelId, (nuint)Unsafe.SizeOf<AssetId>());
                AppDraw.Text(name);
                ImGui.EndDragDropSource();
            }

            //ImGui.SetCursorPosY(i * (ListItemHeight + ListItemPad) + (ListItemPad - 1));
            ImGui.PopID();
        }

        return end;

        //var top = ImGui.GetCursorPosY();
        //var yOffset = (rowHeight - fontSize) * 0.5f;
        //ImGui.SetCursorPosY(top + yOffset);
    }

    private void OnListItemClick(FileDisplayItem it)
    {
        if (it.Binding == FileBinding.Unknown)
        {
            _state.EnqueueDirectory(_assetBrowser.GetChildFolderName(it.FolderIndex));
            return;
        }

        if (!it.FileId.IsValid()) return;

        if (!FileRegistry.TryGetByRootFileId(it.FileId, out var assetId)) return;

        State.EnqueueEvent(new SelectionEvent(assetId));
        _selectedFile = it.FileId;
    }

    private void DragDrop()
    {
        if (!ImGui.IsMouseReleased(ImGuiMouseButton.Left)) return;

        var payload = ImGui.GetDragDropPayload();
        if (!payload.IsNull && payload.IsDataType("ASSET_MODEL"u8))
        {
            var modelId = *(AssetId*)payload.Data;
            if (!modelId.IsValid()) return;

            var model = Assets.Get<Model>(modelId);
            var camera = CameraManager.Instance.Camera;
            var transform = new Transform(camera.Translation + camera.Forward * 10);
            SceneManager.Instance.SpawnFrom(model, in transform);
        }
    }
*/
/*
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
*/