#region

using System.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Editor.ViewModel;
using ConcreteEngine.Shared.TransformData;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor.Gui.Components;

internal static class EntitiesComponent
{
    private struct EntityDataState
    {
        public readonly int EntityId;
        public int ModelId;
        public int MaterialTagKey;
        public TransformDataState Transform;
        public readonly TransformData BaseTransform;

        public EntityDataState(int entityId, in EntityDataPayload payload)
        {
            EntityId = entityId;
            ModelId = payload.Model.ModelId;
            MaterialTagKey = payload.Model.MaterialTagKey;
            BaseTransform = payload.Transform;
            Transform.FromStable(in payload.Transform);
        }
    }

    private const int RowHeight = 32;
    private const int ColumnWidth = 28;

    private static EntityListViewModel ViewModel => StateCtx.EntityListViewModel;

    private static EntityDataState _selectedState = default;
    private static ref TransformDataState TransformState => ref _selectedState.Transform;
    private static ref readonly TransformData BaseTransform => ref _selectedState.BaseTransform;

    
    private static void OnSelectEntity(int entityId)
    {
        if(entityId == ViewModel.SelectedEntityId) return;
        ViewModel.SelectedEntityId = entityId;

        if (entityId == 0)
        {
            _selectedState = default;
            return;
        }
        
        EditorService.OnFetchEntityData(entityId, out var payload);
        _selectedState = new EntityDataState(entityId, in payload);

    }

    private static void OnUpdateTranslation(EntityViewModel entity)
    {
        TransformState.GetDataModel(in BaseTransform.Rotation, out var data);
        var payload = new EntityTransformPayload(entity.EntityId, in data);
        StateCtx.ExecuteSetEntityTransform(in payload);
    }

    private static void OnUpdateScale(EntityViewModel entity)
    {
        TransformState.GetDataModel(in BaseTransform.Rotation, out var data);
        var payload = new EntityTransformPayload(entity.EntityId, in data);
        StateCtx.ExecuteSetEntityTransform(in payload);
    }

    private static void OnUpdateRotation(EntityViewModel entity)
    {
        var rotation = RotationMath.EulerDegreesToQuaternion(in TransformState.EulerAngles);
        TransformState.GetDataModel(in rotation, out var data);
        var payload = new EntityTransformPayload(entity.EntityId, in data);
        StateCtx.ExecuteSetEntityTransform(in payload);
    }

    public static void Draw()
    {
        ImGui.SeparatorText("Entities");

        if (ImGui.BeginChild("#EntityList"))
        {
            DrawEntityList();
            ImGui.EndChild();
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
        //Span<char> buffer = stackalloc char[8];
        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer8);

        foreach (var entity in ViewModel.Entities)
        {
            var selected = entity.EntityId == ViewModel.SelectedEntityId;

            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, GuiTheme.SelectedColor);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, GuiTheme.SelectedColor);
            ImGui.PushStyleColor(ImGuiCol.Header, GuiTheme.PrimaryColor);

            ImGui.PushID(entity.EntityId);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var bufferStr = formatter.Format(entity.EntityId);
            if (EntitySelectable(bufferStr, selected))
            {
                OnSelectEntity(entity.EntityId);

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

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar();
        }
    }


    private static void DrawAssetFilePopupContent(EntityViewModel entity)
    {
        ImGui.SeparatorText("Model");
        ImGui.TextUnformatted("ModelId");
        if (ImGui.InputInt("##model-id", ref _selectedState.ModelId, 0, 0, ImGuiInputTextFlags.None))
        {
        }

        ImGui.TextUnformatted("MaterialTagKey");
        if (ImGui.InputInt("##mat-tag", ref _selectedState.MaterialTagKey, 0, 0, ImGuiInputTextFlags.None))
        {
        }

        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Transform");

        ImGui.TextUnformatted("Translation");
        ImGui.Separator();
        if (ImGui.InputFloat3("##translation", ref TransformState.Translation, "%.3f", ImGuiInputTextFlags.None))
        {
            OnUpdateTranslation(entity);
        }

        ImGui.TextUnformatted("Scale");
        ImGui.Separator();
        if (ImGui.InputFloat3("##scale", ref TransformState.Scale, "%.3f", ImGuiInputTextFlags.None))
        {
            OnUpdateScale(entity);
        }

        ImGui.TextUnformatted("Rotation");
        ImGui.Separator();
        if (ImGui.InputFloat3("##rotation", ref TransformState.EulerAngles, "%.3f", ImGuiInputTextFlags.None))
        {
            OnUpdateRotation(entity);
        }
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