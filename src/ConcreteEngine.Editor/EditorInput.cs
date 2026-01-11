using System.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor;

internal static class EditorInput
{
    private enum DragState : byte
    {
        None = 0,
        DragStart = 1,
        Dragging = 2,
        DragEnd = 3,
    }

    private static Vector2 _prevMousePos;
    private static Vector3 _dragStart;

    private static DragState _dragState;
    private static bool _wasDragging;

    public static bool IsInteracting()
    {
        return ImGui.IsMouseDragging(ImGuiMouseButton.Left) || ImGui.IsItemClicked(ImGuiMouseButton.Left);
    }

    public static void CheckHotkeys(StateManager states)
    {
        if (ImGui.IsItemFocused()) return;

        if (ImGui.IsKeyDown(ImGuiKey.Key1)) states.SetLeftSidebarState(LeftSidebarMode.Assets);
        else if (ImGui.IsKeyDown(ImGuiKey.Key2)) states.SetLeftSidebarState(LeftSidebarMode.Scene);
        else if (ImGui.IsKeyDown(ImGuiKey.Key3)) states.SetRightSidebarState(RightSidebarMode.Camera);
        else if (ImGui.IsKeyDown(ImGuiKey.Key4)) states.SetRightSidebarState(RightSidebarMode.World);
        else if (ImGui.IsKeyDown(ImGuiKey.Key5)) states.SetRightSidebarState(RightSidebarMode.Sky);
        else if (ImGui.IsKeyDown(ImGuiKey.Key6)) states.SetRightSidebarState(RightSidebarMode.Terrain);
    }


    public static void UpdateMouse(float delta, SceneObjectId selectedId, ModelStateHub stateHub)
    {
        var mousePos = ImGui.GetMousePos();
        var deltaAbs = Vector2.Abs(mousePos - _prevMousePos);
        var isLeftClick = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        var isRightClick = ImGui.IsMouseClicked(ImGuiMouseButton.Right);
        var isDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);

        if (isRightClick)
        {
            stateHub.SceneStateComponent.TriggerEvent(EventKey.SelectionChanged, SceneObjectId.Empty);
            return;
        }

        if (isLeftClick && !isDragging)
        {
            HandleClick(selectedId,mousePos, stateHub);
            return;
        }

        switch (_dragState)
        {
            case DragState.None:
                var startDrag = !_wasDragging && isDragging;
                if (startDrag && HandleClick(selectedId,mousePos, stateHub))
                    _dragState = DragState.DragStart;
                break;
            case DragState.DragStart:
                _dragState = isDragging ? DragState.Dragging : DragState.None;
                break;
            case DragState.Dragging:
                _dragState = isDragging ? DragState.Dragging : DragState.DragEnd;
                break;
            case DragState.DragEnd:
                _dragState = DragState.None;
                break;
        }

        switch (_dragState)
        {
            case DragState.None: break;
            case DragState.DragStart:
                if (!HandleDragStart(mousePos)) _dragState = DragState.None;
                else HandleDrag(selectedId,mousePos);
                break;
            case DragState.Dragging:
                if (deltaAbs.X > 0 || deltaAbs.Y > 0) HandleDrag(selectedId,mousePos);
                break;
            case DragState.DragEnd:
                _dragStart = default;
                break;
        }

        _wasDragging = isDragging;
        _prevMousePos = mousePos;
    }

    private static bool HandleClick(SceneObjectId selectedId,Vector2 mousePos, ModelStateHub stateHub)
    {
        var sceneObjectId = EngineController.InteractionController.Raycast(mousePos);
        if (!sceneObjectId.IsValid())
        {
            if (selectedId.IsValid())
                stateHub.SceneStateComponent.TriggerEvent(EventKey.SelectionChanged, SceneObjectId.Empty);

            return false;
        }

        if (sceneObjectId.Id == selectedId) return true;

        stateHub.SceneStateComponent.TriggerEvent(EventKey.SelectionChanged, sceneObjectId);
        return true;
    }

    private static bool HandleDragStart(Vector2 mousePos)
    {
        var pointOnTerrain = EngineController.InteractionController.RaycastTerrain(mousePos);
        if (pointOnTerrain == default) return false;
        _dragStart = pointOnTerrain;
        return true;
    }

    private static void HandleDrag(SceneObjectId selectedId, Vector2 mousePos)
    {
        var newPos = EngineController.InteractionController.RaycastEntityOnTerrain(selectedId, mousePos, _dragStart);
        if (newPos == default) return;

        if (StoreHub.SelectedProxy is { } proxy)
        {
            var property = proxy.GetSpatialProperty();
            var spatial = property.GetValue();
            spatial.Transform.Translation = newPos;
            proxy.GetSpatialProperty().SetValue(spatial);
        }

        //EngineController.CommitSceneObject();
    }
}