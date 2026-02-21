using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Widgets;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.EditorConsts;

namespace ConcreteEngine.Editor.Panels;

internal sealed class SceneListPanel : EditorPanel
{
    private static SearchStringUtf8 _inputUtf8;

    private readonly SceneController _controller;

    private readonly ClipDrawer _clipDrawer;

    private readonly EnumCombo<SceneObjectKind> _sceneKindCombo =
        EnumCombo<SceneObjectKind>.MakeFromCache(defaultName: "All");

    private SceneObjectKind _selectedKind;
    private int _sceneCount;
    private readonly SceneObjectId[] _sceneIds = new SceneObjectId[SceneCapacity];


    public SceneListPanel(PanelContext context, SceneController controller) : base(PanelId.SceneList, context)
    {
        _controller = controller;
        _clipDrawer = new ClipDrawer(DrawListItem);
    }

    public override void Enter()
    {
        if (_sceneCount == 0) TriggerSearch();
    }

    private void OnCategoryChange(SceneObjectKind kind)
    {
        if (_selectedKind == kind) return;
        _selectedKind = kind;
        TriggerSearch();
    }

    public override void Draw(in FrameContext ctx)
    {
        const ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.CharsNoBlank;
        // search
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.65f);

        if (ImGui.InputText("##search-scene"u8, ref _inputUtf8.GetInputRef(), SearchStringUtf8.Length, inputFlags))
            TriggerSearch();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.35f);

        if (_sceneKindCombo.Draw((int)_selectedKind, out var kind))
            OnCategoryChange(kind);

        var count = _sceneCount;

        var layout = TextLayout.Make()
            .TitleSeparator(ref WriteFormat.WriteTitleId(ctx.Sw, "SceneObjects"u8, count), padUp: false);

        // list table
        if (ImGui.BeginTable("scene-list"u8, 3, GuiTheme.TableFlags))
        {
            layout.Row("Kind"u8).Row("Id"u8, GuiTheme.IdColWidth).RowStretch("Name"u8);

            _clipDrawer.Draw(count, GuiTheme.ListPaddedRowHeight, in ctx);

            ImGui.EndTable();
        }
    }

    private void DrawListItem(int i, in FrameContext ctx)
    {
        var id = _sceneIds[i];
        _controller.GetSceneObjectHeader(id, out var header);

        var selected = id == Context.SelectedSceneId;

        ImGui.PushID(id);
        ImGui.TableNextRow();

        TextLayout.Make(GuiTheme.ListRowHeight, TextAlignMode.VerticalCenter)
            .ColumnColor(in StyleMap.GetSceneColor(header.Kind), ref ctx.Sw.Write(header.Kind.ToText()))
            .SelectableColumn(ref ctx.Sw.Write(id), selected, GuiTheme.IdColWidth, out var clicked)
            .Column(ref ctx.Sw.Write(header.Name));

        if (clicked)
            Context.EnqueueEvent(new SceneObjectEvent(id));

        ImGui.PopID();
    }

    private void TriggerSearch()
    {
        _sceneIds.AsSpan(0, _sceneCount).Clear();

        var searchStr = _inputUtf8.GetSearchString(out var key, out var mask);

        var search = new SearchPayload<SceneObjectId>(searchStr, _sceneIds, key, mask);
        var filter = SearchFilter.MakeScene(_selectedKind);

        _sceneCount = _controller.FilterQuery(in search, filter,
            static (in search, filter, in it) =>
            {
                return SearchQuery(in search, filter, in it);
            });
    }

    private static bool SearchQuery(in SearchPayload<SceneObjectId> search, SearchFilter filter, in SceneObjectItem it)
    {
        if (filter.Enabled.HasValue && filter.Enabled != it.Enabled)
            return false;

        if (search.SearchKey > 0 && (it.NameKey & search.SearchMask) != search.SearchKey)
            return false;

        if (search.SearchString.Length > 8 && !it.Name.StartsWith(search.SearchString, StringComparison.Ordinal))
            return false;

        return true;
    }
}