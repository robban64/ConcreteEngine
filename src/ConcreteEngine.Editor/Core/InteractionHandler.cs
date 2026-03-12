using System.Numerics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;
using Silk.NET.Input;

namespace ConcreteEngine.Editor.Core;

internal sealed class InteractionHandler(InteractionController interaction, StateContext ctx)
{
    private static Vector2 MousePos => EditorInputState.Input.Mouse.Position;
    private InteractionMouseState _mouseState;
    
    public void Update()
    {
        if (EditorInputState.Input.IsKeyDown(Key.Escape))
        {
            _mouseState.ResetState();
            return;
        }

        if (!EditorInputState.IsBlockingMouse && !UpdateMouseClick(EditorInputState.InputStateToggles))
            UpdateDrag(EditorInputState.InputStateToggles.IsDragging);

        _mouseState.WasDragging = EditorInputState.InputStateToggles.IsDragging;
    }

    public void DrawGizmo()
    {
        if (ctx.SelectedSceneObject is not { } inspector) return;

        var gizmoEnable = _mouseState.DragState == DragState.None && 
                          !EditorInputState.Input.IsKeyDown(Key.ControlLeft);
                         // !ImGui.IsItemHovered() &&

        EditorCamera.Instance.DrawGizmos(gizmoEnable, inspector);

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
            ctx.EnqueueEvent(new SceneObjectEvent(SceneObjectId.Empty));
    }

    private bool OnClickViewport(Vector2 mousePos)
    {
        var selectedId = ctx.Selection.SelectedSceneId;
        var sceneObjectId = interaction.Raycast(mousePos);
        if (!sceneObjectId.IsValid())
        {
            if (selectedId.IsValid())
                ctx.EnqueueEvent(new SceneObjectEvent(SceneObjectId.Empty));

            return false;
        }

        if (sceneObjectId.Id == selectedId) return true;

        ctx.EnqueueEvent(new SceneObjectEvent(sceneObjectId));

        return true;
    }

    private bool RaycastTerrain(Vector2 mousePos, out Vector3 point)
    {
        point = interaction.RaycastTerrain(mousePos);
        return point != default;
    }

    private void OnDragTerrain(Vector2 mousePos, Vector3 origin)
    {
        var id = ctx.Selection.SelectedSceneId;
        var newPos = interaction.RaycastEntityOnTerrain(id, mousePos, origin);
        if (newPos == default || ctx.Selection.SelectedSceneObject is not { } inspector) return;

        inspector.SceneObject.Translation = newPos;
    }
}