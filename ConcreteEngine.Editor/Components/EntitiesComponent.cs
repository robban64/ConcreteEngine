#region

using System.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor.Components;

internal static class EntitiesComponent
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 36;
    
    private static EntityViewState State => ModelManager.EntitiesStateContext.State!;


    private static ReadOnlySpan<EditorEntityResource> EntitySpan => EditorManagedStore.EntityResourceSpan;


    public static void Draw()
    {
        State.BeforeDraw();
        ImGui.SeparatorText("Entities");
        DrawEntityList();
    }


    private static void DrawEntityList()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoBordersInBody |
                                      ImGuiTableFlags.ScrollY;
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(12, 0));

        if (!ImGui.BeginTable("##entity_list_tbl", 3, flags)) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Model", ImGuiTableColumnFlags.WidthFixed, ColumnWidth);

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Id");

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Name");

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Model");

        DrawList();

        ImGui.PopStyleVar();
        ImGui.EndTable();
    }


    private static unsafe void DrawList()
    {
        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer8);

        var rowHeight = ImGui.GetFrameHeight();
        var clipper = new ImGuiListClipper();
        ImGuiNative.ImGuiListClipper_Begin(&clipper, EntitySpan.Length, rowHeight);

        while (ImGuiNative.ImGuiListClipper_Step(&clipper) != 0)
        {
            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                DrawListItem(i, formatter);
        }

        ImGuiNative.ImGuiListClipper_End(&clipper);
    }

    private static void DrawListItem(int i, NumberSpanFormatter formatter)
    {
        var entity = EntitySpan[i];
        var selected = entity.Id == EditorDataStore.State.SelectedEntity;
        //if (selected) _selectedIndex = i;

        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

        ImGui.PushID(entity.Id);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        var bufferStr = formatter.Format(entity.Id);
        if (EntitySelectable(bufferStr, selected))
            State.SetSelectedEntity(entity.Id);

        ImGui.TableNextColumn();
        var name = entity.Name.Length > 0 ? entity.Name : entity.DisplayName;
        GuiUtils.CenterAlignText(name, RowHeight);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignText(formatter.Format(entity.Model), RowHeight);

        ImGui.PopID();
        ImGui.PopStyleVar();
    }

    public static void DrawProperties()
    {
        if (!EditorDataStore.State.SelectedEntity.IsValid) return;
        if (ImGui.BeginChild("##right-sidebar-properties", new Vector2(0, 0),
                ImGuiChildFlags.AlwaysAutoResize | ImGuiChildFlags.AlwaysUseWindowPadding))
        {
            DrawPropertyContent();
            ImGui.EndChild();
        }
    }

    private static void DrawPropertyContent()
    {
        ref var state = ref EditorDataStore.State.EntityState;
        ref var transform = ref state.Transform;
        var fieldStatus = new ImGuiFieldStatus();
        int modelId = 0;
        int materialId = 0;

        ImGui.SeparatorText("Model");
        ImGui.TextUnformatted("ModelId");
        ImGui.InputInt("ent-prop##model-id", ref modelId, 0, 0, ImGuiInputTextFlags.None);
        //fieldStatus.NextField();

        ImGui.TextUnformatted("Material");
        ImGui.InputInt("ent-prop##mat-id", ref materialId, 0, 0, ImGuiInputTextFlags.None);
        //fieldStatus.NextField();


        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Transform");

        ImGui.TextUnformatted("Translation");
        ImGui.Separator();
        ImGui.InputFloat3("ent-prop##translation", ref transform.Translation, "%.3f", ImGuiInputTextFlags.None);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Scale");
        ImGui.Separator();
        ImGui.InputFloat3("ent-prop##scale", ref transform.Scale, "%.3f", ImGuiInputTextFlags.None);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Rotation");
        ImGui.Separator();
        ImGui.InputFloat3("ent-prop##rotation", ref transform.EulerAngles, "%.3f", ImGuiInputTextFlags.None);
        var rotationField = fieldStatus.NextField();

        if (fieldStatus.HasEdited(out var field)) State.UpdateTransform(field, rotationField);
    }

/*
    private static void DrawAssetFilePopupContent()
    {
        ref var state = ref DataState;
        ref var transform = ref state.Transform;
        var fieldStatus = new ImGuiFieldStatus();
        int modelId = 0;
        int materialId = 0;

        ImGui.SeparatorText("Model");
        ImGui.TextUnformatted("ModelId");
        ImGui.InputInt("##model-id", ref modelId, 0, 0, ImGuiInputTextFlags.None);
        //fieldStatus.NextField();

        ImGui.TextUnformatted("Material");
        ImGui.InputInt("##mat-id", ref materialId, 0, 0, ImGuiInputTextFlags.None);
        //fieldStatus.NextField();


        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Transform");

        ImGui.TextUnformatted("Translation");
        ImGui.Separator();
        ImGui.InputFloat3("##translation", ref transform.Translation, "%.3f", ImGuiInputTextFlags.None);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Scale");
        ImGui.Separator();
        ImGui.InputFloat3("##scale", ref transform.Scale, "%.3f", ImGuiInputTextFlags.None);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Rotation");
        ImGui.Separator();
        ImGui.InputFloat3("##rotation", ref transform.EulerAngles, "%.3f", ImGuiInputTextFlags.None);
        _rotationField = fieldStatus.NextField();

        if (fieldStatus.HasEdited(out var field)) _editedField = field;
    }
*/
    private static bool EntitySelectable(ReadOnlySpan<char> str, bool selected)
    {
        const ImGuiSelectableFlags flags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        var textWidth = ImGui.CalcTextSize(str).X;
        var offset = (ColumnWidth - textWidth) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        return ImGui.Selectable(str, selected, flags, new Vector2(0, RowHeight));
    }
}