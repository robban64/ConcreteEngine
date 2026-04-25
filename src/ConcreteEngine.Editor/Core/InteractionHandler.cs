using System.Numerics;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Data;
using Silk.NET.Input;

namespace ConcreteEngine.Editor.Core;

internal sealed class InteractionHandler(StateManager state)
{
    private InteractionMouseState _mouseState;

    private readonly InteractionController _interactionController = EngineObjectStore.InteractionController;
    private readonly EditorCamera _editorCamera = EditorCamera.Instance;
    private readonly InputController _inputController = EditorInputState.Input;

    private Vector2 MousePos => _inputController.Mouse.Position;

    public void Update()
    {
        if (_inputController.IsKeyDown(Key.Escape))
        {
            _mouseState.ResetState();
            return;
        }

        var inputStateToggles = EditorInputState.InputStateToggles;

        if (!inputStateToggles.IsBlockingMouse && !UpdateMouseClick(inputStateToggles))
            UpdateDrag(inputStateToggles.IsDragging);

        _mouseState.WasDragging = inputStateToggles.IsDragging;
    }

    public void DrawGizmo()
    {
        if (state.Selection.SelectedSceneObject is not { } inspector) return;

        var gizmoEnable = _mouseState.DragState == DragState.None &&
                          !_inputController.IsKeyDown(Key.ControlLeft);

        _editorCamera.DrawGizmos(gizmoEnable, state.Context.Tool, inspector);
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
        var id = state.Selection.SelectedSceneObject?.Id ?? SceneObjectId.Empty;
        var newPos = _interactionController.RaycastEntityOnTerrain(id, mousePos, origin);
        if (newPos == default || state.Selection.SelectedSceneObject is not { } inspector) return;

        inspector.SceneObject.Translation = newPos;
    }
}