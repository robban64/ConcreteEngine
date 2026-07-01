using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.App.Shared;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Widgets;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Scene;

internal sealed unsafe class SceneWindow : EditorWindow
{
    private const float ListFramePad = 4f;
    private const float ListItemHeight = 24f;
    private const float ListItemPaddedHeight = ListItemHeight + ListFramePad;

    private readonly SceneBrowser _browser;
    private readonly ComboInput _kindCombo;
    private readonly TextInput _searchInput;

    private SceneObjectKind _selectedKind;

    private NativeString _title;
    private NativeString _searchString;
    
    private SceneObjectId SelectedId => State.Context.Selection.SelectedSceneId;

    public override ReadOnlySpan<byte> Id => WindowRoot.LeftWindowId;

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
    
    
    protected override void OnCreate()
    {
        _title = StringArena.AllocateString(24);
        _searchString = StringArena.AllocateString(8);

        _searchInput.SetTextBuffer(_searchString);
        if (_browser.FilteredCount == 0) Search(Span<byte>.Empty);
    }
    
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
        _title.NewWrite.Append("SceneObjects [").Append(_browser.FilteredCount).Append(']').End();
    }

    protected override void OnDraw()
    {
        ImGui.SeparatorText(_title);

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
        ImGui.PushStyleColor(ImGuiCol.Button, 0);
        if (ImGui.BeginChild("scene-list"u8))
        {
            DrawListClipper();
        }

        ImGui.EndChild();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }

    private void DrawListClipper()
    {
        var itemWidth = ImGui.GetContentRegionAvail().X - ListItemPaddedHeight - 4f;
        
        ImGuiListClipper clipper = default;
        clipper.Begin(_browser.FilteredCount, ListItemHeight);
        foreach (var range in ClipperEnumerator.New(&clipper))
        {
            DrawList(range, itemWidth);
        }
    }

    private void DrawList(Range32 range, float itemWidth)
    {
        var selectedId = SelectedId;
        uint eyeIcon = StyleMap.GetIntIcon(Icons.Eye), eyeClosedIcon = StyleMap.GetIntIcon(Icons.EyeClosed);

        var cursor = 0;
        var sceneIds = _browser.GetSceneIds(range.Offset, range.Length);
        foreach (var it in _browser.GetDrawEnumerator(range.Offset, range.Length))
        {
            var id = sceneIds[cursor++];
            ImGui.PushID(id);

            if (ImGui.Selectable(it.DisplayName, id == selectedId, 0, new Vector2(itemWidth, ListItemHeight)))
                State.EnqueueEvent(new SelectionEvent(id));

            ImGui.SameLine();
            if (ImGui.Button(it.Visible ? (byte*)&eyeIcon : (byte*)&eyeClosedIcon, new Vector2(ListItemHeight)))
            {
                //it.Visible = !it.Visible;
            }
            ImGui.PopID();
        }
    }
}