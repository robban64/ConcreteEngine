using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Core.Engine.Scene;
using Silk.NET.Input;

namespace ConcreteEngine.Editor.Core;

internal sealed class InteractionHandler(StateManager state, SelectionManager selection)
{
    public Vector3 DragStart;
    public bool WasDragging;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        if (EditorInput.Layer.IsKeyDown(Key.Escape))
        {
            WasDragging = false;
            DragStart = Vector3.Zero;
            EditorInput.DragState = DragState.None;
            return;
        }

        if (!EditorInput.IsBlockingMouse && !UpdateMouseClick(EditorInput.State))
            UpdateDrag(EditorInput.State.IsDragging);

        WasDragging = EditorInput.State.IsDragging;
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
                OnClickViewport(EngineInput.Mouse.ViewportPos);
                return true;
            default:
                return false;
        }
    }

    private void UpdateDrag(bool isDragging)
    {
        var mousePos = EngineInput.Mouse.ViewportPos;
        var dragState = EditorInput.DragState;

        switch (dragState)
        {
            case DragState.None:
                var startDrag = !WasDragging && isDragging;
                if (startDrag && OnClickViewport(mousePos))
                    dragState = DragState.DragStart;
                break;
            case DragState.DragStart:
                dragState = isDragging ? DragState.Dragging : DragState.None;
                break;
            case DragState.Dragging:
                dragState = isDragging ? DragState.Dragging : DragState.DragEnd;
                break;
            case DragState.DragEnd:
                dragState = DragState.None;
                break;
            default:
                Throwers.Unreachable(nameof(dragState));
                break;
        }

        switch (dragState)
        {
            case DragState.None: break;
            case DragState.DragStart:
                if (!RaycastTerrain(mousePos, out var dragStart))
                {
                    dragState = DragState.None;
                    break;
                }

                DragStart = dragStart;
                OnDragTerrain(mousePos, dragStart);
                break;
            case DragState.Dragging:
                OnDragTerrain(mousePos, DragStart);
                break;
            case DragState.DragEnd:
                DragStart = default;
                break;
            default:
                Throwers.Unreachable(nameof(dragState));
                break;
        }

        EditorInput.DragState = dragState;
    }

    private void OnRightClickViewport()
    {
        if (state.Context.Selection.SelectedSceneId.IsValid())
            state.EnqueueEvent(new SelectionEvent(SceneObjectId.Empty));
    }

    private bool OnClickViewport(Vector2 mousePos)
    {
        var selectedId = state.Context.Selection.SelectedSceneId;
        var sceneObject = SceneManager.Instance.Raycaster.GetSceneObjectFromView(mousePos, out _, out _);
        if (sceneObject is null)
        {
            if (selectedId.IsValid())
                state.EnqueueEvent(new SelectionEvent(SceneObjectId.Empty));

            return false;
        }

        if (sceneObject.Id != selectedId)
            state.EnqueueEvent(new SelectionEvent(sceneObject.Id));

        return true;
    }

    private bool RaycastTerrain(Vector2 mousePos, out Vector3 point)
    {
        point = SceneManager.Instance.Raycaster.GetPointOnTerrain(mousePos, out _);
        return point != default;
    }

    private void OnDragTerrain(Vector2 mousePos, Vector3 origin)
    {
        var id = selection.SelectedSceneObject?.Id ?? SceneObjectId.Empty;
        var newPos = SceneManager.Instance.Raycaster.RaycastEntityOnTerrain(id, mousePos, origin);
        if (newPos == default || selection.SelectedSceneObject is not { } inspector) return;

        inspector.Transform.Translation = newPos;
    }
}