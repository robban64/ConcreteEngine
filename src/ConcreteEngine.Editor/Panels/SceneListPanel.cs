using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class SceneListPanel : EditorPanel
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 36;

    private readonly ClipDrawer<ISceneObject> _clipDrawer;

    public SceneListPanel() : base(PanelId.SceneList)
    {
        _clipDrawer = new ClipDrawer<ISceneObject>(DrawListItem);
    }

    public override void Draw(ref FrameContext ctx)
    {
        ImGui.SeparatorText("Scene"u8);

        // Table
        if (!ImGui.BeginTable("##scene-list"u8, 2, GuiTheme.TableFlags))
            return;

        TextLayout.Make().Row("Id"u8, ColumnWidth).RowStretch("Name"u8);
        ImGui.TableHeadersRow();

        var span = EngineController.SceneController.GetSceneObjectSpan();
        DurationProfileTimer.Default.Begin();

        _clipDrawer.Draw(span.Length, RowHeight, span, ref ctx);
        DurationProfileTimer.Default.EndPrintSimple();

        ImGui.EndTable();
    }


    private void DrawListItem(int i, ISceneObject sceneObject, ref FrameContext ctx)
    {
        var selected = sceneObject.Id.IsValid() && sceneObject.Id == Context.SelectedSceneId;

        ImGui.PushID(sceneObject.Id);
        ImGui.TableNextRow(ImGuiTableRowFlags.None, RowHeight);

        new TextLayout(RowHeight, TextAlignMode.VerticalCenter)
            .SelectableColumn(ctx.Sw.Write(sceneObject.Id.Id), selected, ColumnWidth, out var clicked)
            .Column(ctx.Sw.Write(sceneObject.Name));

        if (clicked)
            Context.EnqueueEvent(new SceneObjectEvent(sceneObject.Id));

        ImGui.PopID();
    }
}