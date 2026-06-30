using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
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
using static ConcreteEngine.Editor.EditorConsts;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class SceneWindow : EditorWindow
{
    private const float ListFramePad = 4f;
    private const float ListItemHeight = 24f;
    private const float ListItemPaddedHeight = ListItemHeight + ListFramePad;

    private readonly SceneBrowser _browser;
    private readonly ComboInput _kindCombo;
    private readonly TextInput _searchInput;

    private SceneObjectKind _selectedKind;

    private RangeU16 _titleStrHandle;
    private RangeU16 _inputStrHandle;

    private MemoryBlockPtr _memory;

    private NativeView<byte> TitleStr => _memory.SliceData(_titleStrHandle);
    private NativeView<byte> InputStr => _memory.SliceData(_inputStrHandle);
    private SceneObjectId SelectedId => State.Context.Selection.SelectedSceneId;

    public SceneWindow(StateManager state) : base(state)
    {
        _browser = new SceneBrowser();
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

    private void Search(Span<byte> text)
    {
        _browser.Search(text, SceneObjectKind.Empty);
        SyncState();
    }

    private void SyncState()
    {
        TitleStr.Writer().Append("SceneObjects ["u8).Append(_browser.FilteredCount).Append(']').End();
    }

    protected override void OnCreate()
    {
        var allocator = TextBuffers.PersistentArena.MakeBuilder();
        _inputStrHandle = allocator.AllocSlice(8).AsRange16();
        _titleStrHandle = allocator.AllocSlice(24).AsRange16();
        _memory = TextBuffers.PersistentArena.CommitBuilder(allocator);

        _searchInput.SetTextBuffer(InputStr);
        if (_browser.FilteredCount == 0) Search(Span<byte>.Empty);
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

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(ListFramePad));
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0, 0.5f));
        ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
        if (ImGui.BeginChild("scene-list"u8))
        {
            var clipper = new ImGuiListClipper();
            clipper.Begin(_browser.FilteredCount, ListItemHeight);
            while (clipper.Step())
            {
                DrawList(clipper.DisplayStart, clipper.DisplayEnd - clipper.DisplayStart);
            }

            clipper.End();
        }

        ImGui.EndChild();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }

    private void DrawList(int start, int length)
    {
        if ((uint)start + (uint)length > (uint)_browser.FilteredCount) Throwers.InvalidArgument(nameof(length));

        var sw = TextBuffers.GetWriter();
        uint eyeIcon = StyleMap.GetIntIcon(Icons.Eye), eyeClosedIcon = StyleMap.GetIntIcon(Icons.EyeClosed);

        var size = new Vector2(ImGui.GetContentRegionAvail().X - ListItemPaddedHeight - 4f, ListItemHeight);

        var selectedId = SelectedId;
        foreach (var it in _browser.GetDrawEnumerator(start, length))
        {
            ImGui.PushID(it.Id);

            var nameStr = sw.PadRight(1).AppendIcon(StyleMap.GetIcon(it.Kind.ToIcon()))
                .PadRight(4).Append((byte*)&it.DisplayName)
                .End();

            if (ImGui.Selectable(nameStr, it.Id == selectedId, 0, size))
                State.EnqueueEvent(new SelectionEvent(it.Id));

            ImGui.SameLine();
            if (ImGui.Button(it.Visible ? (byte*)&eyeIcon : (byte*)&eyeClosedIcon, new Vector2(ListItemHeight)))
            {
                //it.Visible = !it.Visible;
            }
            ImGui.PopID();
        }
    }
}