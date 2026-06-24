using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Widgets;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Theme.Palette32;
using static ConcreteEngine.Editor.Theme.StyleMap;

namespace ConcreteEngine.Editor.UI.Assets;

internal sealed unsafe class AssetListPanel : EditorPanel
{
    private const float ListItemHeight = 24f;
    private static float ListItemPad => GuiTheme.CellPadding.X * 2f;

    private static AssetStore Assets => AssetManager.Assets;
    private static AssetFileRegistry FileRegistry => AssetManager.FileRegistry;

    // Temp solution
    public static AssetId RenamedAsset;

    private static readonly Vector2 ListItemSelectSize = new(0, ListItemHeight);

    private readonly AssetListState _state;
    private readonly AssetBrowser _assetBrowser;

    private readonly TextInput _searchInput;
    private readonly ComboInput _assetCombo;

    private RangeU16 _breadcrumbStrHandle;

    private AssetFileId _selectedFile;

    private NativeView<byte> BreadcrumbStr => DataPtr.Slice(_breadcrumbStrHandle);

    private int TotalDrawCount => _state.FilteredCount;

    public AssetListPanel(StateManager state) : base(StateEnums.AssetList, state)
    {
        _assetBrowser = new AssetBrowser();
        _state = new AssetListState(_assetBrowser, AssetKind.Texture);
        _searchInput = new TextInput("search", 8)
            .WithFilter(TextInputFilter.None, allowEmpty: true)
            .WithTransformer(trimmed: true, lowercase: true)
            .WithCallbackU8((searchString) => _state.SetSearch(searchString));

        _assetCombo = ComboInput.MakeFromEnumCache<AssetKind>("asset-combo");
        _assetCombo.StartAt = 1;
        _assetCombo.Layout = FieldLayout.None;
    }

    public override void OnCreate()
    {
        _state.Memory = TextBuffers.PersistentArena.Alloc(AssetListState.NameListCapacity);
        _assetBrowser.BuildFullDirectory();
    }

    private void UpdateTitleText()
    {
        var dirSpan = _assetBrowser.CurrentDirectory.AsSpan();
        var sw = BreadcrumbStr.Writer();
        sw.Append('[').Append(_state.FilteredCount).Append(']').PadRight(2).Append('/');
        foreach (var range in dirSpan.Split('/'))
            sw.Append(dirSpan[range]).Append('/');

        // remove last '/'
        sw.SetCursor(sw.Cursor - 1);
        sw.Append((char)0);
    }


    public override void OnEnter(NativeAllocator allocator)
    {
        _selectedFile = State.Context.Selection.HasAsset
            ? AssetManager.Instance.GetAssetRootFile(State.Context.Selection.SelectedAssetId).Id
            : default;

        _searchInput.SetTextBuffer(allocator.AllocSlice(8));
        _breadcrumbStrHandle = allocator.AllocSlice(64).AsRange16();
        Refresh();
    }

    public override void OnLeave()
    {
        _searchInput.UnsetTextBuffer();
        BreadcrumbStr.Clear();
    }

    private void Refresh()
    {
        _assetCombo.Value = _state.PendingKind != 0 ? (int)_state.PendingKind : (int)_assetBrowser.CurrentKind;
        UpdateTitleText();
        RenamedAsset = default;
    }

    public override void OnDraw()
    {
        var isRootPath = _assetBrowser.IsRootPath;

        if (_state.Sync(RenamedAsset))
            Refresh();

        // Row 1
        if (isRootPath) ImGui.BeginDisabled(true);

        if (ImGui.ArrowButton("##PrevFolder"u8, ImGuiDir.Left))
            _state.EnqueueDirectory(AssetListState.GoBackString);

        if (isRootPath) ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.SeparatorText(BreadcrumbStr);

        // Row 2
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.62f);
        _searchInput.Draw();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.38f);
        if (_assetCombo.Draw())
            _state.EnqueueNewAssetKind((AssetKind)_assetCombo.Value.X);

        // List
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0, 0.5f));
        if (ImGui.BeginTable("asset-list"u8, 1, GuiTheme.ListTableFlags))
        {
            ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
            DrawList();
            ImGui.EndTable();
            DragDrop();
        }

        ImGui.PopStyleVar();
    }

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
/*
            if (kind == AssetKind.Model && binding == FileBinding.RootFile && ImGui.BeginDragDropSource())
            {
                AssetManager.FileRegistry.TryGetByRootFileId(it.FileId, out var modelId);
                ImGui.SetDragDropPayload("ASSET_MODEL"u8, &modelId, (nuint)Unsafe.SizeOf<AssetId>());
                AppDraw.Text(name);
                ImGui.EndDragDropSource();
            }
*/
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