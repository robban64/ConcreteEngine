using ConcreteEngine.Core.Diagnostics.Time;
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

    //private readonly byte[] _searchBuffer = new byte[64];
    private string _searchString = string.Empty;
    // private  readonly Color4[] KindColors = [Palette.TextMuted, Palette.Model, Palette.CyanLight];

    public SceneListPanel(PanelContext context, SceneController controller) : base(PanelId.SceneList, context)
    {
        _controller = controller;
        _clipDrawer = new ClipDrawer(DrawListItem);
    }

    public override void Draw(in FrameContext ctx)
    {
        // search
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.65f);
        if (ImGui.InputText("##input"u8, ref _searchString, 64, ImGuiInputTextFlags.CharsNoBlank)) { }

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.35f);

        if (_sceneKindCombo.Draw((int)_selectedKind, ctx.Writer, out var kind))
            _selectedKind = kind;

        int count = _controller.GetCountByKind(_selectedKind);

        var layout = TextLayout.Make()
            .TitleSeparator(WriteFormat.WriteTitleId(ctx.Writer, "SceneObjects"u8, count), padUp: false);

        // list table
        if (ImGui.BeginTable("##scene-list"u8, 3, GuiTheme.TableFlags))
        {
            layout.Row("Kind"u8).Row("Id"u8, GuiTheme.IdColWidth).RowStretch("Name"u8);

            DurationProfileTimer.Default.Begin();
            _clipDrawer.Draw(count, GuiTheme.ListPaddedRowHeight, in ctx);
            DurationProfileTimer.Default.EndPrintSimple();

            ImGui.EndTable();
        }
    }


    private void DrawListItem(int i, in FrameContext ctx)
    {
        var header = _controller.GetSceneObjectHeader(i);

        var selected = header.Id == ctx.SelectedSceneId;
        var sw = new SpanWriter(ctx.Buffer);

        ImGui.PushID(header.Id.Id);
        ImGui.TableNextRow();

        TextLayout.Make(GuiTheme.ListRowHeight, TextAlignMode.VerticalCenter)
            .ColumnColor(header.Kind.ToColor(), header.Kind.ToText8())
            .SelectableColumn(sw.Write(header.Id.Id), selected, GuiTheme.IdColWidth, out var clicked)
            .Column(sw.Write(header.Name));

        if (clicked)
            Context.EnqueueEvent(new SceneObjectEvent(header.Id));

        ImGui.PopID();
    }
}