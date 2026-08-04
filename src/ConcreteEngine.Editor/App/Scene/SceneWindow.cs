using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Utils;
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

    private SceneObjectId SelectedId => State.Context.Selection.SelectedSceneId;

    public override ReadOnlySpan<byte> Id => WindowRoot.LeftWindowId;

    public SceneWindow(StateManager state) : base(state)
    {
        _browser = new SceneBrowser();
        _kindCombo = ComboInput.Create("scene-combo", SceneObjectKindExt.Values, SceneObjectKindExt.Names, v => OnCategoryChange((SceneObjectKind)v));

        _kindCombo.LabelPlacement = LabelPlacement.None;
        _kindCombo.SetItemName(0, "All");

        _searchInput = new TextInput("search", 8, Search) { AllowEmpty = true, Trim = true, Lowercase = true };
    }


    protected override void OnCreate()
    {
        _title = StringArena.AllocateString(24);
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
        _title.OverWriter.Append("SceneObjects [").Append(_browser.FilteredCount).Append(']').End();
    }

    protected override void OnDraw()
    {
        _kindCombo.Value = (int)_selectedKind;
        ImGui.SeparatorText(_title);

        // search
        var width = ImGui.GetContentRegionAvail().X;
        ImGui.SetNextItemWidth(width * 0.65f);
        _searchInput.Draw();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.35f);
        _kindCombo.Draw();
        ImGui.Separator();

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(ListFramePad));
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0, 0.5f));
        ImGui.PushStyleColor(ImGuiCol.Button, 0);
        if (ImGui.BeginChild("scene-list"u8))
        {
            var itemWidth = ImGui.GetContentRegionAvail().X - ListItemHeight;
            foreach (var range in AppDraw.Clipper(_browser.FilteredCount, ListItemHeight, out _))
                DrawList(range, itemWidth);
        }

        ImGui.EndChild();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }

    private void DrawList(Range32 range, float itemWidth)
    {
        var cursor = 0;
        var selectedId = SelectedId;
        var sceneIds = _browser.GetSceneIds(range.Offset, range.Length);
        foreach (var name in _browser.GetDrawEnumerator(range.Offset, range.Length))
        {
            var id = sceneIds[cursor++];
            var visible = SceneManager.SceneStore.Get(id).Visible;
            ImGui.PushID(id);

            if (ImGui.Selectable(name, id == selectedId, 0, new Vector2(itemWidth, ListItemHeight)))
                State.EnqueueEvent(new SelectionEvent(id));

            ImGui.SameLine();
            if (AppDraw.Button(visible ? IconNames.Eye : IconNames.EyeClosed))
                State.EnqueueEvent(new SceneObjectEvent(id, Visible: !visible));

            ImGui.PopID();
        }
    }
}