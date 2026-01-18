using System.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Components.Draw;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components;

internal sealed class SceneComponent : EditorComponent
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 36;

    private readonly ClipDrawer _clipDrawer;
    public readonly SceneState State;

    public SceneComponent()
    {
        State = new SceneState();
        _clipDrawer = new ClipDrawer(DrawListItem);
    }

    public override void DrawLeft(ref FrameContext ctx)
    {
        ImGui.SeparatorText("Scene"u8);

        // Table
        if (!ImGui.BeginTable("##scene_table"u8, 5, GuiTheme.TableFlags))
            return;

        TextLayout.Make().Row("Id"u8, ColumnWidth).Row("Enabled"u8, ColumnWidth).RowStretch("Name"u8)
            .Row("G"u8, ColumnWidth).Row("R"u8, ColumnWidth);
        ImGui.TableHeadersRow();

        var len = State.GetSceneObjectSpan().Length;
        _clipDrawer.Draw(len, RowHeight, ref ctx);

        ImGui.EndTable();
    }

    public override void DrawRight(ref FrameContext ctx)
    {
        var proxy = ctx.StateCtx.Selection.SceneProxy;
        if (proxy == null) return;

        if (!ImGui.BeginChild("##right-sidebar-properties"u8, ImGuiChildFlags.AlwaysUseWindowPadding))
            return;

        TextLayout.Make()
            .TitleSeparator(SpanWriterUtil.WriteTitleId(ref ctx.Sw, "Scene Object"u8, proxy.Id), Vector2.Zero)
            .Property("Name:"u8, ctx.Sw.Write(proxy.Name))
            .Property("GID:"u8, ctx.Sw.Write(proxy.GIdString))
            .RowSpace();

        foreach (var property in proxy.Properties)
        {
            switch (property)
            {
                case ProxyPropertyEntry<SpatialProperty> spatial:
                    DrawSceneProperty.DrawTransform(State, spatial);
                    break;
                case ProxyPropertyEntry<SourceProperty> renderProp:
                    DrawSceneProperty.DrawRenderProperty(renderProp, ref ctx);
                    break;
                case ProxyPropertyEntry<ParticleProperty> particle:
                    DrawSceneProperty.DrawParticleProperty(State, ref ctx);
                    break;
                case ProxyPropertyEntry<AnimationProperty>:
                    DrawSceneProperty.DrawAnimationProperty(State, ref ctx);
                    break;
            }
        }

        ImGui.EndChild();
    }

    private void DrawListItem(int i, ref FrameContext ctx)
    {
        var sceneObject = State.GetSceneObjectSpan()[i];
        var selected = sceneObject.Id.IsValid() && sceneObject.Id == ctx.StateCtx.Selection.SelectedSceneId;
        ref var sw = ref ctx.Sw;

        ImGui.PushID(sceneObject.Id);
        ImGui.TableNextRow(ImGuiTableRowFlags.None, RowHeight);

        var layout = new TextLayout(RowHeight)
            .SelectableColumn(sw.Write(sceneObject.Id.Id), selected, ColumnWidth, out var clicked);

        layout.WithLayout(TextAlignMode.Center)
            .Column(StrUtils.BoolToYesNoShort(sceneObject.Enabled));

        layout.WithLayout(TextAlignMode.VerticalCenter)
            .Column(sw.Write(sceneObject.Name));

        layout.WithLayout(TextAlignMode.Center)
            .Column(sw.Write(sceneObject.GameEntitiesCount))
            .Column(sw.Write(sceneObject.RenderEntitiesCount));

        if (clicked)
            TriggerEvent(new SceneObjectEvent(EventKey.SelectionChanged, sceneObject.Id));

        ImGui.PopID();
    }
}