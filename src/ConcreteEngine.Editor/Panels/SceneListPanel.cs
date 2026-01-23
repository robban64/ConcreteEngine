using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class SceneListPanel : EditorPanel
{
    private const int RowHeight = 32;
    private const int PaddedROwHeight = 36;
    private const int ColumnWidth = 36;

    private readonly ClipDrawer<ISceneObject> _clipDrawer;

    private readonly SceneController _controller;

    public SceneListPanel(PanelContext context, SceneController controller) : base(PanelId.SceneList, context)
    {
        _controller = controller;
        _clipDrawer = new ClipDrawer<ISceneObject>(DrawListItem);
    }

    public override void Draw( in FrameContext ctx)
    {
        ImGui.SeparatorText("Scene"u8);

        // Table
        if (!ImGui.BeginTable("##scene-list"u8, 2, GuiTheme.TableFlags))
            return;

        TextLayout.Make().Row("Id"u8, ColumnWidth).RowStretch("Name"u8);
        ImGui.TableHeadersRow();

        var span = _controller.GetSceneObjectSpan();
        _clipDrawer.Draw(span.Length, PaddedROwHeight, span, in ctx);

        ImGui.EndTable();
    }


    private void DrawListItem(int i, ISceneObject sceneObject, in FrameContext ctx)
    {
        var id = sceneObject.Id;
        var selected = id == ctx.SelectedSceneId;
        var sw = ctx.Writer;

        ImGui.PushID(id);
        ImGui.TableNextRow();

        new TextLayout(RowHeight, TextAlignMode.VerticalCenter)
            .SelectableColumn(sw.Write(id.Id), selected, ColumnWidth, out var clicked)
            .Column(sw.Write(sceneObject.Name));

        if (clicked)
            Context.EnqueueEvent(new SceneObjectEvent(id));

        ImGui.PopID();
    }
}