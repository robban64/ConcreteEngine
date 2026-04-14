using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Theme.Palette32;
using static ConcreteEngine.Editor.Theme.StyleMap;

namespace ConcreteEngine.Editor.UI.Assets;

internal sealed unsafe class AssetListPanel : EditorPanel
{
    private const ImGuiInputTextFlags InputFlags = ImGuiInputTextFlags.CharsNoBlank;

    private const float ListItemHeight = 24f;
    private static float ListItemPad => GuiTheme.CellPadding.X * 2f;

    private static AssetProvider Provider => EngineObjectStore.AssetProvider;

    // Temp solution
    public static AssetId RenamedAsset;

    private static readonly Vector2 ListItemSelectSize = new(0, ListItemHeight);

    private readonly AssetListState _state;
    private readonly AssetBrowser _assetBrowser;

    private ComboField _assetCombo = null!;

    private NativeView<byte> _inputStr = NativeView<byte>.MakeNull();
    private NativeView<byte> _breadcrumbStr = NativeView<byte>.MakeNull();

    private int TotalDrawCount => _state.FilteredCount;

    public AssetListPanel(StateContext context) : base(PanelId.AssetList, context)
    {
        _assetBrowser = new AssetBrowser();
        _state = new AssetListState(_assetBrowser, AssetKind.Texture);
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
            
        var builder = CreateAllocBuilder();
        _inputStr = builder.AllocSlice(8);
        _breadcrumbStr = builder.AllocSlice(64);
        _state.NameList = builder.AllocSlice(AssetListState.NameListCapacity);
        PanelMemory = builder.Commit();

        _assetBrowser.BuildFullDirectory();
    }

    private void UpdateTitleText()
    {
        var dirSpan = _assetBrowser.CurrentDirectory.AsSpan();
        var sw = _breadcrumbStr.Writer();
        sw.Append('[').Append(_state.FilteredCount).Append(']').PadRight(2).Append('/');
        foreach (var range in dirSpan.Split('/'))
            sw.Append(dirSpan[range]).Append('/');

        // remove last '/'
        sw.SetCursor(sw.Cursor - 1);
        sw.Append((char)0);
    }


    public override void OnEnter() => Refresh();
    public override void OnLeave() => _breadcrumbStr.Clear();

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
        ImGui.SeparatorText(_breadcrumbStr);

        // Row 2
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.62f);
        if (ImGui.InputText("##search-asset"u8, _inputStr, 8, InputFlags))
            OnSearch();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.38f);
        _assetCombo.Draw();
        
        // List
        if (ImGui.BeginTable("asset-list"u8, 1, GuiTheme.ListTableFlags))
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
        clipper.Begin(TotalDrawCount, ListItemHeight + ListItemPad);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, end = clipper.DisplayEnd;
            var currentKind = _assetBrowser.CurrentKind;
            var indices = _state.GetSearchIndices();
            for (var i = 0; i < 4; i++)
            {
                var (icon, color) = GetIconAndColor((FileSpecBinding)i, currentKind);
                ImGui.PushStyleColor(ImGuiCol.Text, color);
                start = DrawList(start, end, icon, (FileSpecBinding)i, indices);
                ImGui.PopStyleColor();
            }
        }

        clipper.End();
    }

    private int DrawList(int start, int end, uint icon, FileSpecBinding binding, UnsafeSpan<byte> indices)
    {
        const ImGuiSelectableFlags selectFlags = ImGuiSelectableFlags.AllowDoubleClick;

        if ((uint)start >= (uint)end) return start;

        for (var i = start; i < end; i++)
        {
            var name = _state.GetDrawData(indices[i], out var it);
            if (it.Binding != binding) return i;

            ImGui.PushID(it.FileId);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            if (ImGui.Selectable("##select"u8, false, selectFlags, ListItemSelectSize))
                OnListItemClick(it);

            ImGui.SetCursorPosY(i * (ListItemHeight + ListItemPad) + (ListItemPad - 1));

            ImGui.TextUnformatted((byte*)&icon);
            ImGui.SameLine();
            AppDraw.Text(name);
            ImGui.PopID();
        }

        return end;

        //var top = ImGui.GetCursorPosY();
        //var yOffset = (rowHeight - fontSize) * 0.5f;
        //ImGui.SetCursorPosY(top + yOffset);
    }

    private void OnListItemClick(FileDisplayItem it)
    {
        if (it.Binding == FileSpecBinding.Unknown)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(it.FileId, 0);
            _state.EnqueueDirectory(_assetBrowser.GetChildFolderName(-it.FileId));
            return;
        }

        if (!it.FileId.IsValid()) return;
        //var file = _assetBrowser.CurrentNode.FindChild(fileId);
        if (!Provider.TryGetByRootFile(it.FileId, out var asset)) return;

        Context.EnqueueEvent(new SelectionEvent(asset.Id));
    }


    private void OnSearch()
    {
        if (_inputStr[0] == 0)
        {
            _state.SetSearch(default);
            return;
        }

        var searchString = InputTextUtils.GetSearchString(_inputStr.AsSpan(), stackalloc byte[_inputStr.Length]);
        _state.SetSearch(searchString);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (uint icon, uint color) GetIconAndColor(FileSpecBinding binding, AssetKind kind)
    {
        return binding switch
        {
            FileSpecBinding.Unknown => (GetIntIcon(Icons.Folder), TextPrimary),
            FileSpecBinding.RootFile => (GetIntIcon(kind.ToIcon()), TextLightBlue),
            FileSpecBinding.DependentFile => (GetIntIcon(kind.ToFileIcon()), TextSecondary),
            FileSpecBinding.UnboundFile => (GetIntIcon(Icons.File), TextMuted),
            _ => throw new ArgumentOutOfRangeException(nameof(binding), binding, null)
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