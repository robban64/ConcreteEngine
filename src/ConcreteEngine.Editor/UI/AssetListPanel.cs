using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Extensions;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.EditorConsts;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class AssetListPanel(StateContext context) : EditorPanel(PanelId.AssetList, context)
{
    private const ImGuiInputTextFlags InputFlags = ImGuiInputTextFlags.CharsNoBlank;
    private const float ListRowHeight = 24f;
    private const float ListPaddedRowHeight = 24f + 6f;

    private AssetKind _selectedKind;
    private AssetFileId _selectedFileId;
    private Vector4 _selectedKindColor = Color4.White;

    private NativeViewPtr<byte> _inputStrPtr;
    private NativeViewPtr<byte> _titleStrPtr;
    private ComboField _assetCombo = null!;

    private string? _pendingDirectory;
    private AssetKind _pendingKind = AssetKind.Unknown;

    private readonly AssetProvider _provider = EngineObjectStore.AssetProvider;
    private readonly SceneController _sceneController = EngineObjectStore.SceneController;
    private readonly AssetBrowser _assetBrowser = new(EngineObjectStore.AssetProvider);

    public override void OnCreate()
    {
        _assetCombo = ComboField
            .MakeFromEnumCache<AssetKind>("##asset-combo",
                () => _pendingKind != 0 ? (int)_pendingKind : (int)_selectedKind,
                v => _pendingKind = (AssetKind)v.X
            )
            .WithProperties(FieldGetDelay.VeryHigh, FieldLayout.None)
            .WithPlaceholder("None").WithStartAt(1);
        _assetCombo.Layout = FieldLayout.None;

        var block = AllocatePanelMemory(8 + 64);
        _inputStrPtr = block->AllocSlice(8);
        _titleStrPtr = block->AllocSlice(64);

        _assetBrowser.BuildFullDirectory();
        _pendingDirectory = "textures";
        _pendingKind = AssetKind.Texture;
    }

    public override void OnEnter()
    {
        if (_assetBrowser.TotalCount == 0) ; //SetAssetDirectory("textures");
        _assetCombo.Refresh();
    }

    private void SyncState()
    {
        if (_pendingKind != AssetKind.Unknown && _pendingKind != _selectedKind)
        {
            _selectedKind = _pendingKind;
            _selectedKindColor = StyleMap.GetAssetColor(_selectedKind);
            _pendingKind = AssetKind.Unknown;
            if (_pendingDirectory == null)
            {
                SetAssetDirectory(_selectedKind.ToRootFolder(), true);
                return;
            }
        }

        if (_pendingDirectory is { } pendingDirectory)
        {
            SetAssetDirectory(pendingDirectory, false);
            _pendingDirectory = null;
        }
    }

    public override void OnDraw(FrameContext ctx)
    {
        if (_selectedKind == AssetKind.Unknown && _pendingKind == AssetKind.Unknown)
            _pendingKind = AssetKind.Texture;

        DrawHeader();

        if (ImGui.ArrowButton("prevFolder"u8, ImGuiDir.Left))
            _pendingDirectory ??= "..";

        ImGui.SameLine();
        ImGui.SeparatorText(_titleStrPtr);

        SyncState();
        if (_assetBrowser.TotalCount == 0) return;

        if (ImGui.BeginTable("asset-list"u8, 2, GuiTheme.TableFlags))
        {
            ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed);
            //ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed);
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
                var model = _provider.GetAsset<Model>(modelId);
                var camera = EditorCamera.Instance.Camera;
                _sceneController.SpawnSceneObject(model, new Transform(camera.Translation + camera.Forward * 10));
            }
        }
    }

    private void DrawHeader()
    {
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.62f);
        if (ImGui.InputText("##search-asset"u8, _inputStrPtr, 8, InputFlags)) ;
        //SetAssetDirectory("textures");

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.38f);
        _assetCombo.Draw();
    }

    private void DrawList(FrameContext ctx)
    {
        int folderCount = _assetBrowser.FolderCount, fileCount = _assetBrowser.AssetCount;
        int total = folderCount + fileCount;
        if (total == 0) return;

        var clipper = new ImGuiListClipper();
        clipper.Begin(total, ListPaddedRowHeight);
        while (clipper.Step())
        {
            int end = clipper.DisplayEnd;
            var folders = _assetBrowser.GetSubFolders();
            var entries = _assetBrowser.GetEntries();
            for (int i = clipper.DisplayStart; i < end; i++)
            {
                if (i < folders.Length)
                {
                    ImGui.PushID(-i);
                    DrawFolderRow(folders[i], ctx.Sw);
                    ImGui.PopID();
                }
                else
                {
                    int entryIndex = i - folderCount;
                    var it = entries[entryIndex];

                    ImGui.PushID(it.FileId);
                    DrawFileRow(it, ctx.Sw);
                    ImGui.PopID();
                }
            }
        }

        clipper.End();
    }

    private void DrawFolderRow(string name, UnsafeSpanWriter sw)
    {
        const ImGuiSelectableFlags
            selectFlags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        var cellTop = ImGui.GetCursorPosY();
        if (ImGui.Selectable("##select"u8, false, selectFlags, new Vector2(0, ListRowHeight)))
            _pendingDirectory ??= name;

        GuiLayout.NextAlignTextVerticalTop(cellTop, ListRowHeight);
        ImGui.TextUnformatted(StyleMap.GetIcon(Icons.Folder));

        AppDraw.ColumnVTop(sw.Write(name), cellTop, ListRowHeight);
    }

    private void DrawFileRow(AssetFileDisplayItem it, UnsafeSpanWriter sw)
    {
        const ImGuiSelectableFlags
            selectFlags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        var selected = it.FileId == _selectedFileId;

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

        ImGui.TextColored(
            it.IsAssetRootFile ? _selectedKindColor : Palette.TextMuted,
            StyleMap.GetIcon(_selectedKind.ToIcon())
        );

        AppDraw.ColumnVTop(sw.Write(it.Name), cellTop, ListRowHeight);
    }

    private void SetAssetDirectory(string directory, bool isRoot)
    {
        if (_selectedKind == AssetKind.Unknown) return;

        if (isRoot || directory.IndexOf('/') > 0)
            _assetBrowser.SetDirectory(directory, _selectedKind);
        else if (directory == "..")
            _assetBrowser.SetToParentDirectory();
        else
            _assetBrowser.SetLocalDirectory(directory);

        var sw = _titleStrPtr.Writer();
        var dirSpan = _assetBrowser.CurrentDirectory.AsSpan();
        foreach (var range in dirSpan.Split('/'))
            sw.Append(dirSpan[range]).Append('/');

        // remove last '/'
        sw.SetCursor(sw.Cursor - 1);
        sw.PadRight(4).Append(_selectedKind.ToText()).Append(" ["u8).Append(_assetBrowser.TotalCount).Append(']').End();
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