using System.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Components.Draw;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components;

internal sealed class SceneComponent : EditorComponent<SceneState>
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 36;

    private readonly ClipDrawer _clipDrawer;

    public SceneComponent()
    {
        _clipDrawer = new ClipDrawer(DrawListItem);
    }

    public override void DrawLeft(SceneState state, ref FrameContext ctx)
    {
        ImGui.SeparatorText("Scene"u8);

        // Table
        if (!ImGui.BeginTable("##scene_table"u8, 5, GuiTheme.TableFlags))
            return;

        ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Enabled"u8, ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("G"u8, ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("R"u8, ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableHeadersRow();

        var sw = ctx.Sw;
        var len = state.GetSceneObjectSpan().Length;
        _clipDrawer.Draw(len, RowHeight, ref sw);

        ImGui.EndTable();
    }

    public override void DrawRight(SceneState state, ref FrameContext ctx)
    {
        if (!state.SelectedId.IsValid() || state.Proxy == null) return;

        if (!ImGui.BeginChild("##right-sidebar-properties"u8, ImGuiChildFlags.AlwaysUseWindowPadding))
            return;

        var selection = state.Proxy;
        DrawGui.SeparatorTextId(ref ctx.Sw, "Scene Object"u8, selection.Id);
        DrawSceneProperty.DrawInfo(selection, ref ctx);

        foreach (var property in selection.Properties)
        {
            switch (property)
            {
                case ProxyPropertyEntry<SpatialProperty> spatial:
                    DrawSceneProperty.DrawTransform(state, spatial);
                    break;
                case ProxyPropertyEntry<SourceProperty> renderProp:
                    DrawSceneProperty.DrawRenderProperty(renderProp, ref ctx);
                    break;
                case ProxyPropertyEntry<ParticleProperty> particle:
                    DrawSceneProperty.DrawParticleProperty(state, ref ctx);
                    break;
                case ProxyPropertyEntry<AnimationProperty>:
                    DrawSceneProperty.DrawAnimationProperty(state, ref ctx);
                    break;
            }
        }

        ImGui.EndChild();
    }

    private void DrawListItem(int i, ref SpanWriter sw)
    {
        var sceneObject = State.GetSceneObjectSpan()[i];
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

        ImGui.PushID(sceneObject.Id);
        ImGui.TableNextRow(ImGuiTableRowFlags.None, RowHeight);
        var selected = sceneObject.Id.IsValid() && sceneObject.Id == State.SelectedId;

        ImGui.TableNextColumn();
        if (DrawGui.DrawSelectable(sw.Write(sceneObject.Id.Id), selected, RowHeight, ColumnWidth))
            TriggerEvent(EventKey.SelectionChanged, sceneObject.Id);

        new TextLayout(RowHeight, TextAlignMode.Center)
            .DrawColumn(StrUtils.BoolToYesNoShort(sceneObject.Enabled))
            .WithLayout(TextAlignMode.VerticalCenter).DrawColumn(sw.Write(sceneObject.Name))
            .WithLayout(TextAlignMode.Center)
            .DrawColumn(sw.Write(sceneObject.GameEntitiesCount))
            .DrawColumn(sw.Write(sceneObject.RenderEntitiesCount));


        ImGui.PopID();
        ImGui.PopStyleVar();
    }
}