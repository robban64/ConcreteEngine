#region

using System.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Editor.ViewModel;
using ConcreteEngine.Shared.TransformData;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor.Gui.Components;

internal static class EntitiesComponent
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 28;

    private static int _rotationField = -1;
    private static int _editedField = -1;
    private static int _selectedIndex = -1;


    private static ModelState<EntitiesViewModel> Model => ModelManager.EntitiesState;
    private static EntitiesViewModel ViewModel => Model.State!;

    private static void OnSelectEntity(EntityRecord entity)
    {
        if (entity.EntityId == ViewModel.Data.EntityId) return;
        Model.TriggerEvent(EventKey.SelectionChanged, entity);
    }

    private static void OnUpdateTransform(EntityRecord entity)
    {
        Model.TriggerEvent(EventKey.SelectionUpdated, entity);
    }

    private static void OnUpdateRotation(EntityRecord entity)
    {
        ref var transform = ref ViewModel.DataState.Transform;
        transform.Rotation = RotationMath.EulerDegreesToQuaternion(in transform.EulerAngles);
        Model.TriggerEvent(EventKey.SelectionUpdated, entity);
    }

    public static void Draw()
    {
        _editedField = -1;
        _selectedIndex = -1;
        
        ImGui.SeparatorText("Entities");
        DrawEntityList();

        
        if (_selectedIndex >= 0 && _editedField >= 0)
        {
            var entity = ViewModel.Entities[_selectedIndex];
            if(_editedField == _rotationField)
                OnUpdateRotation(entity);
            else
                OnUpdateTransform(entity);
            
            _editedField = -1;
        }
    }

    private static void DrawEntityList()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.NoBordersInBody;

        if (!ImGui.BeginTable("##entity_list_tbl", 3, flags)) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Comp", ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Transform", ImGuiTableColumnFlags.WidthStretch);
        
        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Id");

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Comp");

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Transform");

        DrawList();

        ImGui.EndTable();
    }

    private static void DrawList()
    {
        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer8);

        for (var i = 0; i < ViewModel.Entities.Count; i++)
        {
            var entity = ViewModel.Entities[i];
            var selected = entity.EntityId == ViewModel.Data.EntityId;
            if (selected) _selectedIndex = i;

            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

            ImGui.PushID(entity.EntityId);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var bufferStr = formatter.Format(entity.EntityId);
            if (EntitySelectable(bufferStr, selected))
            {
                OnSelectEntity(entity);

                var itemMin = ImGui.GetItemRectMin();
                var itemMax = ImGui.GetItemRectMax();
                ImGui.SetNextWindowPos(new Vector2(itemMax.X + 16, itemMin.Y - 32));
                ImGui.OpenPopup(bufferStr);
            }

            ImGui.TableNextColumn();
            bufferStr = formatter.Format(entity.ComponentCount);
            GuiUtils.CenterAlignText(bufferStr, RowHeight);

            ImGui.TableNextColumn();
            GuiUtils.CenterAlignText("text", RowHeight);

            bufferStr = formatter.Format(entity.EntityId);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12, 10));
            ImGui.SetNextWindowSize(new Vector2(250, 0));
            if (ImGui.BeginPopup(bufferStr, ImGuiWindowFlags.NoResize))
            {
                DrawAssetFilePopupContent(entity);
                ImGui.EndPopup();
            }

            ImGui.PopStyleVar();


            ImGui.PopID();
            ImGui.PopStyleVar();
        }
    }


    private static void DrawAssetFilePopupContent(EntityRecord entity)
    {
        ref var state = ref ViewModel.DataState;
        ref var transform = ref state.Transform;
        var fieldStatus = new ImGuiFieldStatus();
        
        ImGui.SeparatorText("Model");
        ImGui.TextUnformatted("ModelId");
        ImGui.InputInt("##model-id", ref state.ModelId, 0, 0, ImGuiInputTextFlags.None);
        //fieldStatus.NextField();

        ImGui.TextUnformatted("MaterialTagKey");
        ImGui.InputInt("##mat-tag", ref state.MaterialTagKey, 0, 0, ImGuiInputTextFlags.None);
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

    private static bool EntitySelectable(ReadOnlySpan<char> str, bool selected)
    {
        const ImGuiSelectableFlags flags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        var textWidth = ImGui.CalcTextSize(str).X;
        var offset = (ColumnWidth - textWidth) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        return ImGui.Selectable(str, selected, flags, new Vector2(0, RowHeight));
    }
}