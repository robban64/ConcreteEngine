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
using ConcreteEngine.Editor.Inspector;
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
    private const float FolderWindowWidth = 150f;

    private const float FolderItemHeight = 20f;

    private const float GridPadding = 8.0f;
    private const float GridInnerSize = 72.0f;
    private const float GridCellSize = GridInnerSize + GridPadding;
    private const float GridIconSize = GuiTheme.IconSizeLarge;

    private static readonly Vector2 ItemSize = new(GridInnerSize);

    private static readonly Vector2 IconBasePos =
        new((GridInnerSize - GridIconSize) * 0.5f, (GridInnerSize * 0.4f) - (GridIconSize * 0.5f));


    private readonly AssetBrowser _assetBrowser;

    private readonly TextInput _searchInput;

    private AssetFileId _selectedFile;
    private AssetId _selectedAssetId;

    private AssetKind _assetFilter;
    private FileBinding _bindingsFilter;

    private NativeString _breadcrumbs;
    private NativeString _searchString;


    public override ReadOnlySpan<byte> Id => WindowRoot.AssetWindowId;

    public AssetsWindow(StateManager state) : base(state)
    {
        _assetBrowser = new AssetBrowser(OnDirectoryChange);

        _searchInput = new TextInput("search", 8)
            .WithFilter(TextInputFilter.None, allowEmpty: true)
            .WithTransformer(trimmed: true, lowercase: true)
            .WithCallbackU16((searchString) => _assetBrowser.Commit(searchString, _bindingsFilter, _assetFilter));
    }

    protected override void OnCreate()
    {
        _breadcrumbs = StringArena.AllocateString(64);
        _searchString = StringArena.AllocateString(8);

        _searchInput.SetTextBuffer(_searchString);

        _assetBrowser.BuildFullDirectory();
    }

    private void SelectAsset(AssetId id)
    {
        _selectedFile = id.IsValid() ? AssetManager.Instance.GetAssetRootFile(id).Id : AssetFileId.Empty;
        _selectedAssetId = id;
    }

    private void OnListItemClick(AssetFileId fileId)
    {
        if (!fileId.IsValid()) return;
        if (!AssetManager.FileRegistry.TryGetFile(fileId, out var file) || !file.AssetRootId.IsValid()) return;
        State.EnqueueEvent(new SelectionEvent(file.AssetRootId));
    }

    //TODO
    private void OnDirectoryChange(AssetBrowser browser)
    {
        UpdateTitleText();

        var searchText = _searchInput.GetTextSpan();
        if (searchText.Length == 0)
        {
            browser.Commit(ReadOnlySpan<char>.Empty, _bindingsFilter, _assetFilter);
            return;
        }

        Span<char> searchTextU16 = stackalloc char[Encoding.UTF8.GetCharCount(searchText)];
        Encoding.UTF8.GetChars(searchText, searchTextU16);
        browser.Commit(searchTextU16, _bindingsFilter, _assetFilter);
    }

    private void UpdateTitleText()
    {
        var path = _assetBrowser.CurrentNode.GetRelativePath();
        var sw = _breadcrumbs.NewWrite;
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

    private void UpdateFilter(FileBinding bindingFilter, AssetKind assetFilter)
    {
        _bindingsFilter = _bindingsFilter == bindingFilter ? FileBinding.Unknown : bindingFilter;
        _assetFilter = _assetFilter == assetFilter ? AssetKind.Unknown : assetFilter;
        OnDirectoryChange(_assetBrowser);
    }

    public override void OnUpdateDiagnostic()
    {
        if (State.Context.Selection.SelectedAssetId != _selectedAssetId)
            SelectAsset(State.Context.Selection.SelectedAssetId);
    }

    protected override void OnDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(3f));

        ImGui.PushStyleColor(ImGuiCol.ChildBg, Palette.MenuBg);
        if (ImGui.BeginChild("asset-toolbar"u8, new Vector2(0, 22), ImGuiChildFlags.None))
        {
            DrawToolbar();
        }

        ImGui.EndChild();
        ImGui.PopStyleColor();

        if (ImGui.BeginChild("folders"u8, new Vector2(FolderWindowWidth, 0), ImGuiChildFlags.None))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0, 0.5f));

            DrawFolders();

            ImGui.PopStyleVar();
        }

        ImGui.EndChild();

        ImGui.SameLine();

        if (ImGui.BeginChild("files"u8, Vector2.Zero, ImGuiChildFlags.None))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            ImGui.PushFont(GuiTheme.TextFont, GuiTheme.FontSizeSmall);

            DrawFiles();

            ImGui.PopFont();
            ImGui.PopStyleVar();
        }

        ImGui.EndChild();

        ImGui.PopStyleVar();
    }

    private void DrawToolbar()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Palette.FrameBgHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Palette.FrameBgActive);

        if (AppDraw.DrawButton(GetIcon(Icons.Cog))) ImGui.OpenPopup("settings"u8);

        ImGui.SameLine();
        if (AppDraw.DrawButton(GetIcon(Icons.Plus))) ImGui.OpenPopup("menu"u8);

        ImGui.SameLine(0.0f, 12.0f);
        if (AppDraw.DrawButton(GetIcon(Icons.ChevronLeft), !_assetBrowser.IsRootPath))
            _assetBrowser.GoToParent();

        //
        ImGui.SameLine();
        //

        ImGui.AlignTextToFramePadding();
        ImGui.TextDisabled(_breadcrumbs);

        const float rightWidth = 150.0f + 128f;
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - WindowPadding.X - rightWidth);

        ImGui.SetNextItemWidth(150.0f);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, SurfaceDark);
        _searchInput.Draw();
        ImGui.PopStyleColor();

        //
        var bindingFilter = _bindingsFilter;
        var assetFilter = _assetFilter;

        ImGui.SameLine();
        if (AppDraw.DrawToggleButton(GetIcon(Icons.File), bindingFilter == FileBinding.RootFile))
            UpdateFilter(FileBinding.RootFile, assetFilter);

        ImGui.SameLine(0, 8f);
        
        if (AppDraw.DrawToggleButton(GetIcon(AssetIcons.ShaderIcon), assetFilter == AssetKind.Shader))
            UpdateFilter(bindingFilter, AssetKind.Shader);

        ImGui.SameLine();
        if (AppDraw.DrawToggleButton(GetIcon(AssetIcons.ModelIcon), assetFilter == AssetKind.Model))
            UpdateFilter(bindingFilter, AssetKind.Model);

        ImGui.SameLine();
        if (AppDraw.DrawToggleButton(GetIcon(AssetIcons.TextureIcon), assetFilter == AssetKind.Texture))
            UpdateFilter(bindingFilter, AssetKind.Texture);

        ImGui.SameLine();
        if (AppDraw.DrawToggleButton(GetIcon(AssetIcons.MaterialIcon), assetFilter == AssetKind.Material))
            UpdateFilter(bindingFilter, AssetKind.Material);


        ImGui.PopStyleColor(3);
    }

    private void DrawFolders()
    {
        var size = new Vector2(0, FolderItemHeight);
        var icon = GetIntIcon(Icons.Folder);

        var sw = TextBuffers.GetWriter();
        var nodes = _assetBrowser.CurrentNode.GetChildren();

        if (nodes.Length == 0)
        {
            ImGui.Selectable("No folders found."u8, false, ImGuiSelectableFlags.Disabled, size);
            return;
        }

        for (var i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            var previewName = node.PreviewName;

            var text = sw
                .AppendIcon((byte*)&icon).PadRight(2)
                .Append((byte*)&previewName).AppendImGuiId(i)
                .End();

            if (ImGui.Selectable(text, false, 0, size))
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
        var fileIds = _assetBrowser.GetFileIds(start, length);

        var gridIndex = 0;
        foreach (var file in _assetBrowser.GetDrawEnumerator(start, length))
        {
            var fileId = fileIds[gridIndex];
            AssetsExtensions.GetIconAndColor(file.Binding, AssetKind.Texture, out var icon, out var color);

            var startPos = ImGui.GetCursorScreenPos(); // top left

            if (ImGui.Selectable(sw.AppendImGuiId(fileId).End(), selectedFileId == fileId, 0, ItemSize))
                OnListItemClick(fileId);

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

            int nextGridIndex = (gridIndex + 1) % columnCount;
            if (nextGridIndex != 0 && gridIndex + 1 < length)
            {
                ImGui.SameLine(0.0f, GridPadding);
            }
            else
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + GridPadding);
                ImGui.Dummy(default);
            }

            gridIndex++;
        }
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