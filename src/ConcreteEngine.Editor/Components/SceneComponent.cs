using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Components.Draw;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.Components;

internal sealed class SceneComponent : EditorComponent<SceneState>
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 36;
    
    public override void DrawRight(SceneState state,in FrameContext ctx)
    {
        if (!StoreHub.SelectedId.IsValid() || state.Proxy == null) return;

        var za = ctx.GetWriter();
        var selection = state.Proxy;

        float childHeight = ImGui.GetContentRegionAvail().Y - 2;
        if (ImGui.BeginChild("##right-sidebar-properties"u8,ImGuiChildFlags.AlwaysUseWindowPadding))
        {
            ImGui.SeparatorText(za.Append("Scene Object ["u8).Append(selection.Id).AppendEnd(")"u8).AsSpan());
            za.Clear();
            DrawSceneProperty.DrawInfo(selection, ref za);
            DrawSceneProperty.DrawTransform(selection.GetSpatialProperty());
            foreach (var property in selection.Properties)
            {
                switch (property)
                {
                    case ProxyPropertyEntry<SourceProperty> renderProp: DrawSceneProperty.DrawRenderProperty(renderProp, ref za); break;
                    case ProxyPropertyEntry<ParticleProperty> partProp: DrawSceneProperty.DrawParticleProperty(partProp, ref za); break;
                    case ProxyPropertyEntry<AnimationProperty> animProp: DrawSceneProperty.DrawAnimationProperty(animProp, ref za); break;
                }
            }

            /*
            var componentRef = EditorDataStore.EntityState.ComponentRef;
            if (!componentRef.IsValid)
            {
                ImGui.EndChild();
                return;
            }

            ImGui.Dummy(new Vector2(0, 4));

            if (componentRef.ItemType == EditorItemType.Animation)
                DrawAnimationProperties();
            else if (componentRef.ItemType == EditorItemType.Particle)
                DrawParticleProperties();
                */
            ImGui.EndChild();
        }
    }


    public override void DrawLeft( SceneState state,in FrameContext ctx)
    {
        ImGui.SeparatorText("Scene"u8);

        // Table
        if (!ImGui.BeginTable("##scene_table"u8, 5, GuiTheme.TableFlags)) return;

        ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Enabled"u8, ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("G"u8, ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("R"u8, ImGuiTableColumnFlags.WidthFixed, ColumnWidth);

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Id"u8);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("En"u8);

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Name"u8);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("G"u8);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("R"u8);

        DrawList(state, in ctx);

        ImGui.EndTable();
    }


    private  void DrawList(SceneState state, in FrameContext ctx)
    {
        var sceneObjects = state.SceneObjects;

        var zaBuilder = ZaUtf8SpanWriter.Create(ctx.Buffer);
        
        var rowHeight = RowHeight + ((ImGui.GetStyle().CellPadding.Y + ImGui.GetStyle().WindowPadding.Y) * 2);
        var clipper = new ImGuiListClipper();
        clipper.Begin(sceneObjects.Length, rowHeight);
        while (clipper.Step())
        {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                DrawListItem(rowHeight, sceneObjects[i], ref zaBuilder);
        }

        clipper.End();
    }

    private void DrawListItem(float height, ISceneObject sceneObject, ref ZaUtf8SpanWriter zaBuilder)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

        ImGui.PushID(sceneObject.Id);
        ImGui.TableNextRow(ImGuiTableRowFlags.None, height);
        var selected = sceneObject.Id.IsValid() && sceneObject.Id == StoreHub.SelectedId;
        //if (selected) _selectedIndex = i;

        zaBuilder.Clear();
        var idSpan = zaBuilder.Append(sceneObject.Id).EndOfBuffer().AsSpan();
        ImGui.TableNextColumn();
        if (ObjectSelectable(idSpan, selected))
            TriggerEvent(EventKey.SelectionChanged, sceneObject.Id);

        zaBuilder.Clear();

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignText(StrUtils.BoolToYesNoShort(sceneObject.Enabled), RowHeight);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextVertical(zaBuilder.AppendEnd(sceneObject.Name).AsSpan(), RowHeight);
        zaBuilder.Clear();
        
        ImGui.TableNextColumn();
        GuiUtils.CenterAlignText(zaBuilder.Append(sceneObject.GameEntitiesCount).EndOfBuffer().AsSpan(), RowHeight);
        zaBuilder.Clear();

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignText(zaBuilder.Append(sceneObject.RenderEntitiesCount).EndOfBuffer().AsSpan(), RowHeight);
        zaBuilder.Clear();


        ImGui.PopID();
        ImGui.PopStyleVar();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ObjectSelectable(ReadOnlySpan<byte> str, bool selected)
    {
        const ImGuiSelectableFlags flags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        var textWidth = ImGui.CalcTextSize(str).X;
        var offset = (ColumnWidth - textWidth) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        return ImGui.Selectable(str, selected, flags, new Vector2(0, RowHeight));
    }
}