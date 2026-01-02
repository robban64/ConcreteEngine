using System.Numerics;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Core;

internal static class EditorInput
{
    private enum DragState : byte
    {
        None = 0,
        DragStart = 1,
        Dragging = 2,
        DragEnd = 3,
    }

    private const float ScrollSensitivity = 1.0f;
    private const float SmoothFactor = 0.2f;

    private static Vector2 _prevMousePos;
    private static Vector3 _dragStart;

    private static DragState _dragState;
    private static bool _wasDragging;

    public static bool IsInteracting()
    {
        return ImGui.IsMouseDragging(ImGuiMouseButton.Left) || ImGui.IsItemClicked(ImGuiMouseButton.Left);
    }

    public static void CheckHotkeys()
    {
        if (ImGui.IsItemFocused()) return;

        if (ImGui.IsKeyDown(ImGuiKey.Key1)) StateContext.SetLeftSidebarState(LeftSidebarMode.Assets);
        else if (ImGui.IsKeyDown(ImGuiKey.Key2)) StateContext.SetLeftSidebarState(LeftSidebarMode.Entities);
        else if (ImGui.IsKeyDown(ImGuiKey.Key3)) StateContext.SetRightSidebarState(RightSidebarMode.Camera);
        else if (ImGui.IsKeyDown(ImGuiKey.Key4)) StateContext.SetRightSidebarState(RightSidebarMode.World);
        else if (ImGui.IsKeyDown(ImGuiKey.Key5)) StateContext.SetRightSidebarState(RightSidebarMode.Sky);
        else if (ImGui.IsKeyDown(ImGuiKey.Key6)) StateContext.SetRightSidebarState(RightSidebarMode.Terrain);
    }


    public static void UpdateMouse(float delta)
    {
        var mousePos = ImGui.GetMousePos();
        var deltaAbs = Vector2.Abs(mousePos - _prevMousePos);
        var isLeftClick = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        var isRightClick = ImGui.IsMouseClicked(ImGuiMouseButton.Right);
        var isDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);

        if (isRightClick)
        {
            EngineController.DeSelectEntity();
            return;
        }

        if (isLeftClick && !isDragging)
        {
            HandleClick(mousePos);
            return;
        }

        switch (_dragState)
        {
            case DragState.None:
                var startDrag = !_wasDragging && isDragging;
                if (startDrag && HandleClick(mousePos))
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
                else HandleDrag(mousePos);
                break;
            case DragState.Dragging:
                if (deltaAbs.X > 0 || deltaAbs.Y > 0) HandleDrag(mousePos);
                break;
            case DragState.DragEnd:
                _dragStart = default;
                break;
        }

        _wasDragging = isDragging;
        _prevMousePos = mousePos;
    }

    private static bool HandleClick(Vector2 mousePos)
    {
        var entity = EngineController.InteractionController.Raycast(mousePos);
        if (!entity.IsValid)
        {
            if (EditorDataStore.SelectedEntity.IsValid)
                ModelManager.EntitiesStateContext.TriggerEvent<EditorEntityResource?>(EventKey.SelectionChanged, null);

            return false;
        }

        var resource = ManagedStore.Get<EditorEntityResource>(entity);
        ModelManager.EntitiesStateContext.TriggerEvent(EventKey.SelectionChanged, resource);
        return true;
    }

    private static bool HandleDragStart(Vector2 mousePos)
    {
        var pointOnTerrain = EngineController.InteractionController.RaycastTerrain(mousePos);
        if (pointOnTerrain == default) return false;
        _dragStart = pointOnTerrain;
        return true;
    }

    private static void HandleDrag(Vector2 mousePos)
    {
        var entity = EditorDataStore.SelectedEntity;
        var newPos = EngineController.InteractionController.RaycastEntityOnTerrain(entity, mousePos, _dragStart);
        if (newPos == default) return;
        EditorDataStore.EntityState.Transform.Translation = newPos;
        EngineController.CommitEntity();
    }
}