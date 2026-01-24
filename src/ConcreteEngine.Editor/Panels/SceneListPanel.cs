using ConcreteEngine.Core.Common.Numerics;
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
    private const int RowHeight = 32;
    private const int PaddedRowHeight = 36;
    private const int ColumnWidth = 36;

    private readonly ClipDrawer _clipDrawer;

    private readonly SceneController _controller;

   // private  readonly Color4[] KindColors = [Palette.TextMuted, Palette.Model, Palette.CyanLight];

    public SceneListPanel(PanelContext context, SceneController controller) : base(PanelId.SceneList, context)
    {
        _controller = controller;
        _clipDrawer = new ClipDrawer(DrawListItem);
    }

    public override void Draw(in FrameContext ctx)
    {
        ImGui.SeparatorText("Scene"u8);

        if (!ImGui.BeginTable("##scene-list"u8, 3, GuiTheme.TableFlags))
            return;

        TextLayout.Make().Row("Id"u8, ColumnWidth).RowStretch("Name"u8).Row("Kind"u8, 46);
        ImGui.TableHeadersRow();

        _clipDrawer.Draw(_controller.Count, PaddedRowHeight, in ctx);

        ImGui.EndTable();
    }


    private void DrawListItem(int i, in FrameContext ctx)
    {
        var header = _controller.GetHeader(i);
        var selected = header.Id == ctx.SelectedSceneId;
        var sw = ctx.Writer;

        ImGui.PushID(header.Id.Id);
        ImGui.TableNextRow();

        TextLayout.Make(RowHeight, TextAlignMode.VerticalCenter)
            .SelectableColumn(sw.Write(header.Id.Id), selected, ColumnWidth, out var clicked)
            .Column(sw.Write(header.Name))
            .ColumnColor(header.Kind.ToColor(), header.Kind.ToText8());

        if (clicked)
            Context.EnqueueEvent(new SceneObjectEvent(header.Id));

        ImGui.PopID();
    }
}