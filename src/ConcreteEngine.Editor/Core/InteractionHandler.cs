using System.Numerics;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using Silk.NET.Input;

namespace ConcreteEngine.Editor.Core;

internal sealed class InteractionHandler(StateContext ctx)
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
        if (ctx.SelectedSceneObject is not { } inspector) return;

        var gizmoEnable = _mouseState.DragState == DragState.None &&
                          !_inputController.IsKeyDown(Key.ControlLeft);

        _editorCamera.DrawGizmos(gizmoEnable, inspector);
    }


    private bool UpdateMouseClick(InputStateToggles inputStateToggles)
    {
        if (inputStateToggles.IsRightClick)
        {
            OnRightClickViewport();
            return true;
        }

        if (inputStateToggles.IsUsingGizmo || inputStateToggles.IsHoveringGizmo) return true;
        if (inputStateToggles is { IsLeftClick: true, IsDragging: false })
        {
            OnClickViewport(MousePos);
            return true;
        }

        return false;
    }

    private void UpdateDrag(bool isDragging)
    {
        ref var mouseState = ref _mouseState;
        switch (mouseState.DragState)
        {
            case DragState.None:
                var startDrag = !mouseState.WasDragging && isDragging;
                if (startDrag && OnClickViewport(MousePos))
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
        }

        switch (mouseState.DragState)
        {
            case DragState.None: break;
            case DragState.DragStart:
                if (!RaycastTerrain(MousePos, out var dragStart))
                {
                    mouseState.DragState = DragState.None;
                    break;
                }

                mouseState.DragStart = dragStart;
                OnDragTerrain(MousePos, dragStart);
                break;
            case DragState.Dragging:
                OnDragTerrain(MousePos, mouseState.DragStart);
                break;
            case DragState.DragEnd:
                mouseState.DragStart = default;
                break;
        }
    }

    private void OnRightClickViewport()
    {
        if (ctx.Selection.SelectedSceneId.IsValid())
            ctx.EnqueueEvent(new SelectionEvent(SceneObjectId.Empty));
    }

    private bool OnClickViewport(Vector2 mousePos)
    {
        var selectedId = ctx.Selection.SelectedSceneId;
        var sceneObjectId = _interactionController.Raycast(mousePos);
        if (!sceneObjectId.IsValid())
        {
            if (selectedId.IsValid())
                ctx.EnqueueEvent(new SelectionEvent(SceneObjectId.Empty));

            return false;
        }

        if (sceneObjectId.Id == selectedId) return true;

        ctx.EnqueueEvent(new SelectionEvent(sceneObjectId));

        return true;
    }

    private bool RaycastTerrain(Vector2 mousePos, out Vector3 point)
    {
        point = _interactionController.RaycastTerrain(mousePos);
        return point != default;
    }

    private void OnDragTerrain(Vector2 mousePos, Vector3 origin)
    {
        var id = ctx.Selection.SelectedSceneId;
        var newPos = _interactionController.RaycastEntityOnTerrain(id, mousePos, origin);
        if (newPos == default || ctx.Selection.SelectedSceneObject is not { } inspector) return;

        inspector.SceneObject.Translation = newPos;
    }
}