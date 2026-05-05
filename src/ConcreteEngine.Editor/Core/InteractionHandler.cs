using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Data;
using Silk.NET.Input;

namespace ConcreteEngine.Editor.Core;

internal sealed class InteractionHandler(StateManager state, SelectionManager selection)
{
    private InteractionMouseState _mouseState;

    private static InputController Input => EditorInputState.Input;
    private static Vector2 MousePos => Input.Mouse.ViewPos;

    private readonly InteractionController _interactionController = EngineObjectStore.InteractionController;

    public bool GizmoEnabled => _mouseState.DragState == DragState.None && !Input.IsKeyDown(Key.ControlLeft);
    
    public void Update()
    {
        if (Input.IsKeyDown(Key.Escape))
        {
            _mouseState.ResetState();
            return;
        }

        var inputState = EditorInputState.State;

        if (!inputState.IsBlockingMouse && !UpdateMouseClick(inputState))
            UpdateDrag(inputState.IsDragging);

        _mouseState.WasDragging = inputState.IsDragging;
    }

    private bool UpdateMouseClick(InputStateToggles inputStateToggles)
    {
        switch (inputStateToggles)
        {
            case { IsRightClick: true }:
                OnRightClickViewport();
                return true;
            case { IsUsingGizmo: true, IsHoveringGizmo: true }:
                return true;
            case { IsLeftClick: true, IsDragging: false }:
                OnClickViewport(MousePos);
                return true;
            default:
                return false;
        }
    }

    private void UpdateDrag(bool isDragging)
    {
        var mousePos = MousePos;
        ref var mouseState = ref _mouseState;
        switch (mouseState.DragState)
        {
            case DragState.None:
                var startDrag = !mouseState.WasDragging && isDragging;
                if (startDrag && OnClickViewport(mousePos))
                    mouseState.DragState = DragState.DragStart;
                break;
            case DragState.DragStart:
                mouseState.DragState = isDragging ? DragState.Dragging : DragState.None;
                break;
            case DragState.Dragging:
                mouseState.DragState = isDragging ? DragState.Dragging : DragState.DragEnd;
                break;
            case DragState.DragEnd:
                mouseState.DragState = DragState.None;
                break;
            default: throw new ArgumentOutOfRangeException();
        }

        switch (mouseState.DragState)
        {
            case DragState.None: break;
            case DragState.DragStart:
                if (!RaycastTerrain(mousePos, out var dragStart))
                {
                    mouseState.DragState = DragState.None;
                    break;
                }

                mouseState.DragStart = dragStart;
                OnDragTerrain(mousePos, dragStart);
                break;
            case DragState.Dragging:
                OnDragTerrain(mousePos, mouseState.DragStart);
                break;
            case DragState.DragEnd:
                mouseState.DragStart = default;
                break;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    private void OnRightClickViewport()
    {
        if (state.Context.Selection.SelectedSceneId.IsValid())
            state.EnqueueEvent(new SelectionEvent(SceneObjectId.Empty));
    }

    private bool OnClickViewport(Vector2 mousePos)
    {
        var selectedId = state.Context.Selection.SelectedSceneId;
        var sceneObjectId = _interactionController.Raycast(mousePos);
        if (!sceneObjectId.IsValid())
        {
            if (selectedId.IsValid())
                state.EnqueueEvent(new SelectionEvent(SceneObjectId.Empty));

            return false;
        }

        if (sceneObjectId != selectedId)
            state.EnqueueEvent(new SelectionEvent(sceneObjectId));

        return true;
    }

    private bool RaycastTerrain(Vector2 mousePos, out Vector3 point)
    {
        point = _interactionController.RaycastTerrain(mousePos);
        return point != default;
    }

    private void OnDragTerrain(Vector2 mousePos, Vector3 origin)
    {
        var id = selection.SelectedSceneObject?.Id ?? SceneObjectId.Empty;
        var newPos = _interactionController.RaycastEntityOnTerrain(id, mousePos, origin);
        if (newPos == default || selection.SelectedSceneObject is not { } inspector) return;

        inspector.Transform.Translation = newPos;
    }
}