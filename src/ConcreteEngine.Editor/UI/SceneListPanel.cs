using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
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

internal sealed unsafe class SceneListPanel : EditorPanel
{
    private const ImGuiTableFlags TableFlags =
        ImGuiTableFlags.ScrollY |
        ImGuiTableFlags.NoPadOuterX |
        ImGuiTableFlags.NoPadInnerX |
        ImGuiTableFlags.SizingFixedFit;


    private const float ListItemHeight = 20f;
    private const float ListItemPad = 4f;

    private static readonly Vector2 VisBtnSize = new(ListItemHeight, ListItemHeight);
    private static readonly Vector2 TableSelectSize = new(0, ListItemHeight);

    private readonly SceneController _controller = EngineObjectStore.SceneController;

    private readonly ComboInput _kindCombo;
    private readonly TextInput _searchInput;

    private readonly SceneObjectId[] _sceneIds = new SceneObjectId[SceneCapacity];
    private SceneObjectKind _selectedKind;
    private int _sceneCount;

    private RangeU16 _titleStrHandle;
    private RangeU16 _inputStrHandle;

    private NativeView<byte> TitleStr => DataPtr.Slice(_titleStrHandle);
    private NativeView<byte> InputStr => DataPtr.Slice(_inputStrHandle);
    private SceneObjectId SelectedId => State.Context.Selection.SelectedSceneId;

    public SceneListPanel(StateManager state) : base(StateEnums.SceneList, state)
    {
        _kindCombo = ComboInput.MakeFromEnumCache<SceneObjectKind>("scene-combo");
        _kindCombo.Layout = FieldLayout.None;
        _kindCombo.SetItemName(0, "All");

        _searchInput = new TextInput("search", 8)
            .WithFilter(TextInputFilter.None, allowEmpty: true)
            .WithTransformer(trimmed: true, lowercase:true)
            .WithCallbackU8(Search);
    }

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

    public override void OnEnter(ref MemoryBlockPtr memory)
    {
        _inputStrHandle = memory.AllocSlice(8).AsRange16();
        _titleStrHandle = memory.AllocSlice(24).AsRange16();
        _searchInput.SetTextBuffer(InputStr);

        if (_sceneCount == 0) Search(Span<byte>.Empty);
    }

    public override void OnLeave()
    {
        _searchInput.UnsetTextBuffer();
    }

    public override void OnDraw()
    {
        // search
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.65f);
        _searchInput.Draw();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.35f);
        if (_kindCombo.Draw())
            OnCategoryChange((SceneObjectKind)_kindCombo.Value.X);

        ImGui.SeparatorText(TitleStr);

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
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)length,(uint)_sceneIds.Length);

        var sw = TextBuffers.GetWriter();

        uint eyeIcon = StyleMap.GetIntIcon(Icons.Eye), eyeClosedIcon = StyleMap.GetIntIcon(Icons.EyeClosed);
        var selectedId = SelectedId;
        foreach (var id in _sceneIds.AsSpan(start, length))
        {
            var it = _controller.GetSceneObject(id);
            var isSelected = id == selectedId;

            ImGui.PushID(id);
            ImGui.TableNextRow();
            
            ImGui.TableNextColumn();
            var nameStr = sw.Append(StyleMap.GetIcon(it.Kind.ToIcon())).PadRight(4).Append(it.Name).End();
            if (ImGui.Selectable(nameStr, isSelected, 0, TableSelectSize))
                State.EnqueueEvent(new SelectionEvent(it.Id));

            ImGui.TableNextColumn();
            var visibleIcon = it.Visible ? (byte*)&eyeIcon : (byte*)&eyeClosedIcon;
            if (ImGui.Button(visibleIcon, VisBtnSize)) it.Visible = !it.Visible;

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
        var span = _controller.GetSceneObjectSpan();
        foreach (var it in span)
        {
            if (count >= AssetCapacity) break;

            if (_selectedKind > SceneObjectKind.Empty && _selectedKind != it.Kind)
                continue;

            if (searchKey <= 0 || searchId == it.Id || (it.PackedName & searchMask) == searchKey)
                _sceneIds[count++] = it.Id;
        }

        _sceneCount = count;

        SyncState();
    }
}