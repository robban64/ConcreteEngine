using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Components.Draw;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

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

    public override void DrawLeft(SceneState state, in FrameContext ctx)
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

        var sw = ctx.Writer;
        var len = state.GetSceneObjectSpan().Length;
        _clipDrawer.Draw(len, RowHeight, ref sw);

        ImGui.EndTable();
    }

    public override void DrawRight(SceneState state, in FrameContext ctx)
    {
        if (!state.SelectedId.IsValid() || state.Proxy == null) return;

        if (!ImGui.BeginChild("##right-sidebar-properties"u8, ImGuiChildFlags.AlwaysUseWindowPadding))
            return;

        var selection = state.Proxy;
        var sw = ctx.Writer;
        DrawContext.SeparatorTextId(ref sw, "Scene Object"u8, selection.Id);
        DrawSceneProperty.DrawInfo(selection, ref sw);

        foreach (var property in selection.Properties)
        {
            switch (property)
            {
                case ProxyPropertyEntry<SpatialProperty> spatial:
                    DrawSceneProperty.DrawTransform(state, spatial);
                    break;
                case ProxyPropertyEntry<SourceProperty> renderProp:
                    DrawSceneProperty.DrawRenderProperty(renderProp, ref sw);
                    break;
                case ProxyPropertyEntry<ParticleProperty> particle:
                    DrawSceneProperty.DrawParticleProperty(state, ref sw);
                    break;
                case ProxyPropertyEntry<AnimationProperty>:
                    DrawSceneProperty.DrawAnimationProperty(state, ref sw);
                    break;
            }
        }

        ImGui.EndChild();
    }

    private void DrawListItem(int i,  ref SpanWriter sw)
    {
        var sceneObject = State.GetSceneObjectSpan()[i];
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

        ImGui.PushID(sceneObject.Id);
        ImGui.TableNextRow(ImGuiTableRowFlags.None, RowHeight);
        var selected = sceneObject.Id.IsValid() && sceneObject.Id == State.SelectedId;

        ImGui.TableNextColumn();
        if (GuiUtils.Selectable(sw.Write(sceneObject.Id.Id), selected, RowHeight, ColumnWidth))
            TriggerEvent(EventKey.SelectionChanged, sceneObject.Id);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignText(StrUtils.BoolToYesNoShort(sceneObject.Enabled), RowHeight);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextVertical(sw.Write(sceneObject.Name), RowHeight);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignText(sw.Write(sceneObject.GameEntitiesCount), RowHeight);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignText(sw.Write(sceneObject.RenderEntitiesCount), RowHeight);


        ImGui.PopID();
        ImGui.PopStyleVar();
    }
}