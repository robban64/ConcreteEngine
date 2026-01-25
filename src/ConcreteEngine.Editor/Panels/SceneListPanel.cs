using System.Diagnostics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class SceneListPanel : EditorPanel
{
    private readonly ClipDrawer _clipDrawer;

    private readonly SceneController _controller;

    private readonly EnumCombo<SceneObjectKind> _sceneKindCombo =
        EnumCombo<SceneObjectKind>.MakeFromCache(defaultName: "All");

    private SceneObjectKind _selectedKind;

    private readonly List<SceneObjectId> _filteredIds = new(512);

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

    public override void Draw(in FrameContext ctx)
    {
        // search
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.65f);

        unsafe
        {
            if (ImGui.InputText("##input"u8, DataStore.SceneInputBuffer32.Ptr, 16, ImGuiInputTextFlags.CharsNoBlank))
            {
                TriggerSearch();
            }
        }

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.35f);

        if (_sceneKindCombo.Draw((int)_selectedKind, ctx.Writer, out var kind))
            OnCategoryChange(kind);

        int count = _filteredIds.Count;

        var layout = TextLayout.Make()
            .TitleSeparator(ref WriteFormat.WriteTitleId(ctx.Writer, "SceneObjects"u8, count), padUp: false);

        // list table
        if (ImGui.BeginTable("##scene-list"u8, 3, GuiTheme.TableFlags))
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

        ImGui.PushID(id.Id);
        ImGui.TableNextRow();

        TextLayout.Make(GuiTheme.ListRowHeight, TextAlignMode.VerticalCenter)
            .ColumnColor(header.Kind.ToColor(), header.Kind.ToText8())
            .SelectableColumn(ref sw.Write(id.Id), selected, GuiTheme.IdColWidth, out var clicked)
            .Column(ref sw.Write(header.Name));

        if (clicked)
            Context.EnqueueEvent(new SceneObjectEvent(id));

        ImGui.PopID();
    }


    private Stopwatch _sw = new();

    private void TriggerSearch()
    {
        _sw.Start();

        var input = DataStore.SceneInputBuffer32.AsSpan();
        var length = StrUtils.SliceNullTerminate(input, out var byteSpan);

        ulong key = 0, mask = 0;
        Span<char> charBuffer = stackalloc char[length];
        if (StrUtils.DecodeUtf8Input(byteSpan, charBuffer, out var searchStr))
        {
            key = StringPacker.Pack(byteSpan);
            mask = StringPacker.GetMask(length);
        }

        var filter = new SceneObjectFilter
        {
            SearchString = searchStr,
            Enabled = null,
            Kind = _selectedKind,
            SearchKey = key,
            SearchMask = mask
        };

        _controller.FilterQuery(_filteredIds, in filter, SearchQuery);
        _sw.Stop();
        Console.WriteLine($"[{searchStr}] - {_sw.ElapsedTicks / 1000.0}");
        _sw.Reset();
    }

    private static bool SearchQuery(in SceneObjectFilter filter, in SceneObjectItem it)
    {
        if (filter.Enabled.HasValue && filter.Enabled != it.Enabled) return false;

        if (filter.SearchKey > 0 && (it.NameKey & filter.SearchMask) != filter.SearchKey) return false;

        return filter.SearchString.Length <= 8 || it.Name.StartsWith(filter.SearchString, StringComparison.Ordinal);
    }
}