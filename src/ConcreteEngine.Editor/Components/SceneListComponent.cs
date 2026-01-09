using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.Components;

internal static class SceneListComponent
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 36;

    private static ModelStateContext Context => ModelManager.SceneStateContext;

    private static ReadOnlySpan<ISceneObject> SceneObjects => EngineController.SceneController.GetSceneObjectSpan();

    public static void Draw()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoBordersInBody |
                                      ImGuiTableFlags.ScrollY;

        ImGui.SeparatorText("Scene"u8);

        // Table
        if (!ImGui.BeginTable("##scene_table"u8, 5, flags)) return;

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

        DrawList();

        ImGui.EndTable();
    }


    private static unsafe void DrawList()
    {
        var sceneObjects = SceneObjects;

        Span<byte> buffer = stackalloc byte[32];
        var zaBuilder = ZaUtf8SpanWriter.Create(buffer);
        
        var rowHeight = RowHeight + ((ImGui.GetStyle().CellPadding.Y + ImGui.GetStyle().WindowPadding.Y) * 2);
        var clipper = new ImGuiListClipper();
        clipper.Begin(sceneObjects.Length, rowHeight);
        while (clipper.Step())
        {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                DrawListItem(rowHeight, sceneObjects[i], zaBuilder);
        }

        clipper.End();
    }

    private static void DrawListItem(float height, ISceneObject sceneObject, ZaUtf8SpanWriter zaBuilder)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

        ImGui.PushID(sceneObject.Id);
        ImGui.TableNextRow(ImGuiTableRowFlags.None, height);
        var selected = sceneObject.Id.IsValid() && sceneObject.Id == StoreHub.SelectedId;
        //if (selected) _selectedIndex = i;

        zaBuilder.Clear();
        var idSpan = zaBuilder.Append(sceneObject.Id).AppendEndOfBuffer().AsSpan();
        ImGui.TableNextColumn();
        if (ObjectSelectable(idSpan, selected))
            Context.TriggerEvent(EventKey.SelectionChanged, sceneObject.Id);

        zaBuilder.Clear();

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignText(StrUtils.BoolToYesNoShort(sceneObject.Enabled), RowHeight);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextVertical(zaBuilder.AppendEnd(sceneObject.Name).AsSpan(), RowHeight);
        zaBuilder.Clear();
        
        ImGui.TableNextColumn();
        GuiUtils.CenterAlignText(zaBuilder.Append(sceneObject.GameEntitiesCount).AppendEndOfBuffer().AsSpan(), RowHeight);
        zaBuilder.Clear();

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignText(zaBuilder.Append(sceneObject.RenderEntitiesCount).AppendEndOfBuffer().AsSpan(), RowHeight);
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