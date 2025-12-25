using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.Components;

internal static class SceneListComponent
{
    private const int RowHeight  = 32;
    private const int ColumnWidth  = 36;

    private static ModelStateContext Context => ModelManager.SceneStateContext;

    public static void Draw()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoBordersInBody |
                                      ImGuiTableFlags.ScrollY ;

        ImGui.SeparatorText("Scene");

        // Table
        if (!ImGui.BeginTable("##scene_table", 5, flags)) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("G", ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("R", ImGuiTableColumnFlags.WidthFixed, ColumnWidth);

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Id");

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("En");

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Name");

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("G");

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("R");

        DrawList();

        ImGui.EndTable();
    }


    private static unsafe void DrawList()
    {
        var sceneObjects = ManagedStore.SceneObjectSpan;

        Span<char> buffer = stackalloc char[16];
        var zaBuilder = ZaSpanStringBuilder.Create(buffer);

        var rowHeight = ImGui.GetFrameHeight();
        var clipper = new ImGuiListClipper();
        ImGuiNative.ImGuiListClipper_Begin(&clipper, sceneObjects.Length, rowHeight);
        while (ImGuiNative.ImGuiListClipper_Step(&clipper) != 0)
        {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                DrawListItem(i, sceneObjects[i], zaBuilder);
        }

        ImGuiNative.ImGuiListClipper_End(&clipper);
    }

    private static void DrawListItem(int i, EditorSceneObject sceneObject, ZaSpanStringBuilder zaBuilder)
    {
        var selected = sceneObject.Id.IsValid && sceneObject.Id == EditorDataStore.SelectedSceneObject;
        //if (selected) _selectedIndex = i;

        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

        ImGui.PushID(sceneObject.Id);
        ImGui.TableNextRow();

        var idSpan = zaBuilder.Append(sceneObject.Id.Identifier).AsSpan();
        ImGui.TableNextColumn();
        if (ObjectSelectable(idSpan, selected))
            Context.TriggerEvent(EventKey.SelectionChanged, sceneObject);

        zaBuilder.Clear();

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignText(StringUtils.BoolToYesNoShort(sceneObject.Enabled), RowHeight);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextVertical(sceneObject.Name, RowHeight);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignText(zaBuilder.Append(sceneObject.GameEcsCount).AsSpan(), RowHeight);
        zaBuilder.Clear();
        
        ImGui.TableNextColumn();
        GuiUtils.CenterAlignText(zaBuilder.Append(sceneObject.RenderEcsCount).AsSpan(), RowHeight);
        zaBuilder.Clear();


        ImGui.PopID();
        ImGui.PopStyleVar();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ObjectSelectable(ReadOnlySpan<char> str, bool selected)
    {
        const ImGuiSelectableFlags flags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        var textWidth = ImGui.CalcTextSize(str).X;
        var offset = (ColumnWidth - textWidth) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        return ImGui.Selectable(str, selected, flags, new Vector2(0, RowHeight));
    }
}