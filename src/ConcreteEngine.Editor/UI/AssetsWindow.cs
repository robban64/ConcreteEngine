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

    private static readonly Vector2 ListItemSelectSize = new(0, ListItemHeight);

    private readonly AssetBrowser _assetBrowser;

    private readonly TextInput _searchInput;
    private readonly ComboInput _assetCombo;

    private AssetFileId _selectedFile;

    private RangeU16 _breadcrumbStrHandle;

    private MemoryBlockPtr _memory;

    private static AvgFrameTimer _avg;

    public AssetsWindow(StateManager state) : base( state)
    {
        _assetBrowser = new AssetBrowser(OnDirectoryChange);

        _searchInput = new TextInput("search", 8)
            .WithFilter(TextInputFilter.None, allowEmpty: true)
            .WithTransformer(trimmed: true, lowercase: true)
            .WithCallbackU16((searchString) => _assetBrowser.SetSearch(searchString));

        _assetCombo = ComboInput.MakeFromEnumCache<AssetKind>("asset-combo");
    }

    public override ReadOnlySpan<byte> Id => WindowRoot.AssetWindowId;

    public override void OnCreate()
    {
        var allocator = TextBuffers.PersistentArena.MakeBuilder();
        var inputHandle = allocator.AllocSlice(8).AsRange16();
        _breadcrumbStrHandle = allocator.AllocSlice(64).AsRange16();
        _memory = TextBuffers.PersistentArena.CommitBuilder(allocator);
        
        _searchInput.SetTextBuffer(_memory.SliceData(inputHandle));

        //_state.Memory = TextBuffers.PersistentArena.Alloc(AssetListState.NameListCapacity);
        _avg.BeginSample();
        _assetBrowser.BuildFullDirectory();
        _avg.EndSample();
        _avg.ResetAndPrint("Build full directory");
    }


/*
    public override void OnEnter()
    {
        _selectedFile = State.Context.Selection.HasAsset
            ? AssetManager.Instance.GetAssetRootFile(State.Context.Selection.SelectedAssetId).Id
            : default;

        Refresh();
    }
*/

    public override void OnUpdateDiagnostic() => Refresh();

    private void Refresh()
    {
        UpdateTitleText();
    }


    private void OnDirectoryChange(AssetBrowser browser)
    {
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
        var sw = _memory.SliceData(_breadcrumbStrHandle).Writer();
        var path = _assetBrowser.CurrentNode.GetRelativePath();
        if (path.Length == 0)
        {
            sw.Write('/');
            return;
        }

        sw.Append('[').Append(_assetBrowser.FilteredCount).Append(']').PadRight(2);
        foreach (var range in path.Split('/'))
            sw.Append(path[range]).Append('/');

        // remove last '/'
        sw.SetCursor(sw.Cursor - 1);
        sw.Append((char)0);
    }


    protected override void OnDraw()
    {
        //if (_state.Sync(RenamedAsset))
        //Refresh();

        // Row 1
        var isRootPath = _assetBrowser.IsRootPath;
        if (isRootPath) ImGui.BeginDisabled(true);

        if (ImGui.ArrowButton("##PrevFolder"u8, ImGuiDir.Left))
            _assetBrowser.GoToParent();

        if (isRootPath) ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.SeparatorText(_memory.SliceData(_breadcrumbStrHandle));

        // Row 2
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.62f);
        _searchInput.Draw();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.38f);
        if (_assetCombo.Draw()) ;
        //_state.EnqueueNewAssetKind((AssetKind)_assetCombo.Value.X);

        // List
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0, 0.5f));
        ImGui.BeginChild("AssetList"u8);
        _avg.BeginSample();
        DrawFolders();
        DrawFiles();
        if (_avg.EndSample() > 160) _avg.ResetAndPrint("Draw-AssetList");
        // DragDrop();
        ImGui.EndChild();

        ImGui.PopStyleVar();
    }

    private void DrawFolders()
    {
        var sw = TextBuffers.GetWriter();
        int folderId = 0;
        var icon = GetIntIcon(Icons.Folder);
        foreach (var it in _assetBrowser.CurrentNode.GetChildren())
        {
            var text = sw.AppendIcon((byte*)&icon).PadRight(2)
                .Append(it.PreviewName.GetReadSpan()).AppendImGuiId(folderId--)
                .End();
            if (ImGui.Selectable(text, false, 0, ListItemSelectSize))
                _assetBrowser.GoToChild(it.GetFolderName());
        }
    }

    private void DrawFiles()
    {
        if(_assetBrowser.FilteredCount == 0) return;
        
        var sw = TextBuffers.GetWriter();

        var selectedFileId = _selectedFile;
        uint icon = 0, color = 0;
        foreach (var file in _assetBrowser.GetFilteredFileEnumerator())
        {
            var style = GetIconAndColor(file.Binding, AssetKind.Texture);
            if (style.icon != icon)
            {
                if (color > 0) ImGui.PopStyleColor();
                ImGui.PushStyleColor(ImGuiCol.Text, style.color);
                color = style.color;
                icon = style.icon;
            }

            var text = sw.AppendIcon((byte*)&icon).PadRight(2)
                .Append(file.LogicalName).AppendImGuiId(file.Id)
                .End();

            var isSelected = selectedFileId == file.Id;
            if (ImGui.Selectable(text, isSelected, 0, ListItemSelectSize))
                OnListItemClick(file.Id);
        }

        ImGui.PopStyleColor();
    }

    private void OnListItemClick(AssetFileId fileId)
    {
        if (!fileId.IsValid()) return;

        if (!AssetManager.FileRegistry.TryGetByRootId(fileId, out var assetId)) return;

        State.EnqueueEvent(new SelectionEvent(assetId));
        _selectedFile = fileId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (uint icon, uint color) GetIconAndColor(FileBinding binding, AssetKind kind)
    {
        return binding switch
        {
            FileBinding.Unknown => (GetIntIcon(Icons.Folder), TextPrimary),
            FileBinding.RootFile => (GetIntIcon(kind.ToIcon()), TextLightBlue),
            FileBinding.DependentFile => (GetIntIcon(kind.ToFileIcon()), TextSecondary),
            FileBinding.UnboundFile => (GetIntIcon(Icons.File), TextMuted),
            _ => Throwers.Unreachable<(uint, uint)>(nameof(binding))
        };
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