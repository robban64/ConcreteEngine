using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Scene;

internal sealed class SceneListPanel : EditorPanel
{
    private readonly ClipDrawer _clipDrawer;

    private readonly SceneController _controller;

    private readonly EnumCombo<SceneObjectKind> _sceneKindCombo =
        EnumCombo<SceneObjectKind>.MakeFromCache(defaultName: "All");

    private SceneObjectKind _selectedKind;

    private readonly List<SceneObjectId> _filteredIds = new(512);

    private static readonly NativeArray<byte> InputBuffer = new(32);

    public SceneListPanel(PanelContext context, SceneController controller) : base(PanelId.SceneList, context)
    {
        _controller = controller;
        _clipDrawer = new ClipDrawer(DrawListItem);
    }

    public override void Enter()
    {
        if (_filteredIds.Count == 0) TriggerSearch();
    }

    private void OnCategoryChange(SceneObjectKind kind)
    {
        if (_selectedKind == kind) return;
        _selectedKind = kind;
        TriggerSearch();
    }

    public override unsafe void Draw(in FrameContext ctx)
    {
        // search
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.65f);

        if (ImGui.InputText("##input"u8, InputBuffer, 16, ImGuiInputTextFlags.CharsNoBlank))
            TriggerSearch();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.35f);

        if (_sceneKindCombo.Draw((int)_selectedKind, ctx.Writer, out var kind))
            OnCategoryChange(kind);

        int count = _filteredIds.Count;

        var layout = TextLayout.Make()
            .TitleSeparator(ref WriteFormat.WriteTitleId(ctx.Writer, "SceneObjects"u8, count), padUp: false);

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
        var id = _filteredIds[i];
        _controller.GetSceneObjectHeader(id, out var header);

        var selected = id == ctx.SelectedSceneId;
        var sw = ctx.Writer;

        ImGui.PushID(id);
        ImGui.TableNextRow();

        TextLayout.Make(GuiTheme.ListRowHeight, TextAlignMode.VerticalCenter)
            .ColumnColor(StyleMap.GetSceneColor(header.Kind), header.Kind.ToText8())
            .SelectableColumn(ref sw.Write(id), selected, GuiTheme.IdColWidth, out var clicked)
            .Column(ref sw.Write(header.Name));

        if (clicked)
            Context.EnqueueEvent(new SceneObjectEvent(id));

        ImGui.PopID();
    }

    private void TriggerSearch()
    {
        var input = InputBuffer.AsSpan();
        var length = StrUtils.SliceNullTerminate(input, out var byteSpan);

        ulong key = 0, mask = 0;
        Span<char> charBuffer = stackalloc char[length];
        if (StrUtils.DecodeUtf8Input(byteSpan, charBuffer, out var searchStr))
        {
            key = StringPacker.Pack(byteSpan);
            mask = StringPacker.GetMask(length);
        }

        var search = new SearchStringPacked(searchStr, key, mask);
        var filter = new SceneObjectFilter(_selectedKind);
        _controller.FilterQuery(_filteredIds, in search, filter, SearchQuery);
    }

    private static bool SearchQuery(in SearchStringPacked search, SceneObjectFilter filter, in SceneObjectItem it)
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