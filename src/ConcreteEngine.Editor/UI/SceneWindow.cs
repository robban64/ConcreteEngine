using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Widgets;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.EditorConsts;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class SceneWindow : EditorWindow
{
    private const ImGuiTableFlags TableFlags =
        ImGuiTableFlags.ScrollY |
        ImGuiTableFlags.NoPadOuterX |
        //ImGuiTableFlags.NoPadInnerX | 
        ImGuiTableFlags.SizingFixedFit;


    private const float ListItemPad = 4f;
    private const float ListItemHeight = 20f;
    private const float ListItemPaddedHeight = ListItemHeight + ListItemPad;

    private readonly ComboInput _kindCombo;
    private readonly TextInput _searchInput;

    private readonly SceneObjectId[] _sceneIds = new SceneObjectId[SceneCapacity];
    private SceneObjectKind _selectedKind;
    private int _sceneCount;

    private RangeU16 _titleStrHandle;
    private RangeU16 _inputStrHandle;

    private MemoryBlockPtr _memory;

    private NativeView<byte> TitleStr => _memory.SliceData(_titleStrHandle);
    private NativeView<byte> InputStr => _memory.SliceData(_inputStrHandle);
    private SceneObjectId SelectedId => State.Context.Selection.SelectedSceneId;

    public SceneWindow(StateManager state) : base(state)
    {
        _kindCombo = ComboInput.MakeFromEnumCache<SceneObjectKind>("scene-combo");
        _kindCombo.Layout = FieldLayout.None;
        _kindCombo.SetItemName(0, "All");

        _searchInput = new TextInput("search", 8)
            .WithFilter(TextInputFilter.None, allowEmpty: true)
            .WithTransformer(trimmed: true, lowercase: true)
            .WithCallbackU8(Search);
    }

    public override ReadOnlySpan<byte> Id => WindowRoot.LeftWindowId;

    private void OnCategoryChange(SceneObjectKind kind)
    {
        if (_selectedKind == kind) return;
        _selectedKind = kind;
        Search(Span<byte>.Empty);
    }

    private void SyncState()
    {
        TitleStr.Writer().Append("SceneObjects ["u8).Append(_sceneCount).Append(']').End();
    }

    protected override void OnCreate()
    {
        var allocator = TextBuffers.PersistentArena.MakeBuilder();
        _inputStrHandle = allocator.AllocSlice(8).AsRange16();
        _titleStrHandle = allocator.AllocSlice(24).AsRange16();
        _memory = TextBuffers.PersistentArena.CommitBuilder(allocator);

        _searchInput.SetTextBuffer(InputStr);
        if (_sceneCount == 0) Search(Span<byte>.Empty);
    }


    protected override void OnDraw()
    {
        ImGui.SeparatorText(TitleStr);

        // search
        var width = ImGui.GetContentRegionAvail().X;
        ImGui.SetNextItemWidth(width * 0.65f);
        _searchInput.Draw();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.35f);
        if (_kindCombo.Draw())
            OnCategoryChange((SceneObjectKind)_kindCombo.Value.X);

        ImGui.Separator();

        // list table
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4f));
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0, 0.5f));
        ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
        if (!ImGui.BeginTable("scene-list"u8, 2, TableFlags)) return;

        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Visible"u8, ImGuiTableColumnFlags.WidthFixed, 28);
        var clipper = new ImGuiListClipper();
        clipper.Begin(_sceneCount, ListItemHeight + ListItemPad);
        while (clipper.Step())
        {
            DrawList(clipper.DisplayStart, clipper.DisplayEnd - clipper.DisplayStart);
        }

        clipper.End();

        ImGui.EndTable();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }

    private void DrawList(int start, int length)
    {
        if ((uint)start + (uint)length > (uint)_sceneIds.Length) Throwers.InvalidArgument(nameof(length));

        var idSpan = new ReadOnlySpan<SceneObjectId>(_sceneIds, start, length);

        var selectedId = SelectedId;
        var sw = TextBuffers.GetWriter();
        uint eyeIcon = StyleMap.GetIntIcon(Icons.Eye), eyeClosedIcon = StyleMap.GetIntIcon(Icons.EyeClosed);

        foreach (var file in SceneManager.SceneStore.MakeSparseEnumerator(idSpan))
        {
            ImGui.PushID(file.Id);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var nameStr = sw.PadRight(1).AppendIcon(StyleMap.GetIcon(file.Kind.ToIcon()))
                .PadRight(4).Append(file.Name)
                .End();

            if (ImGui.Selectable(nameStr, file.Id == selectedId, 0, new Vector2(0, ListItemHeight)))
                State.EnqueueEvent(new SelectionEvent(file.Id));

            ImGui.TableNextColumn();
            if (ImGui.Button(file.Visible ? (byte*)&eyeIcon : (byte*)&eyeClosedIcon, new Vector2(ListItemPaddedHeight)))
                file.Visible = !file.Visible;

            ImGui.PopID();
        }
    }


    private void Search(Span<byte> byteSpan)
    {
        if (_sceneCount > 0)
            _sceneIds.AsSpan(0, _sceneCount).Clear();

        ulong searchKey = 0, searchMask = 0;
        var searchId = 0;

        if (byteSpan.Length > 0)
        {
            searchKey = StringPacker.PackAscii(byteSpan, true);
            searchMask = StringPacker.GetMaskUtf8(byteSpan.Length);

            if (!int.TryParse(byteSpan, out searchId)) searchId = 0;
        }

        var count = 0;
        foreach (var it in SceneManager.SceneStore)
        {
            if (count >= AssetCapacity) break;

            if (_selectedKind > SceneObjectKind.Empty && _selectedKind != it.Kind)
                continue;

            if (searchKey <= 0 || searchId == it.Id.Id || (it.PackedName & searchMask) == searchKey)
                _sceneIds[count++] = it.Id;
        }

        _sceneCount = count;

        SyncState();
    }
}