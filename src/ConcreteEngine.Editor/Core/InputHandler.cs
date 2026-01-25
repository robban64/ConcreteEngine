using System.Numerics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Core;

internal sealed class InputHandler(InteractionController interaction, StateContext ctx)
{
    private EditorMouseState _mouseState;

    public void UpdateMouse()
    {
        if (EditorInput.IsBlockingEngine()) return;

        var inputState = EditorInput.InputState;
        ref var mouseState = ref _mouseState;
        mouseState.MousePos = ImGui.GetMousePos();

        if (!UpdateMouseClick(inputState))
            UpdateDrag(inputState.IsDragging, ref mouseState);

        mouseState.WasDragging = inputState.IsDragging;
        mouseState.PrevMousePos = mouseState.MousePos;
    }

    private bool UpdateMouseClick(EditorInputState inputState)
    {
        if (inputState.IsRightClick)
        {
            OnRightClickViewport();
            return true;
        }

        if (inputState is { IsLeftClick: true, IsDragging: false })
        {
            OnClickViewport(_mouseState.MousePos);
            return true;
        }

        return false;
    }

    private void UpdateDrag(bool isDragging, ref EditorMouseState mouseState)
    {
        switch (mouseState.DragState)
        {
            case DragState.None:
                var startDrag = !mouseState.WasDragging && isDragging;
                if (startDrag && OnClickViewport(mouseState.MousePos))
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
                if (!RaycastTerrain(mouseState.MousePos, out var dragStart))
                {
                    mouseState.DragState = DragState.None;
                    break;
                }

                mouseState.DragStart = dragStart;
                OnDragTerrain(mouseState.MousePos, dragStart);
                break;
            case DragState.Dragging:
                OnDragTerrain(mouseState.MousePos, mouseState.DragStart);
                break;
            case DragState.DragEnd:
                mouseState.DragStart = default;
                break;
        }
    }


    public void OnRightClickViewport()
    {
        if (ctx.Selection.SelectedSceneId.IsValid())
            ctx.EnqueueEvent(new SceneObjectEvent(SceneObjectId.Empty));
    }

    public bool OnClickViewport(Vector2 mousePos)
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
        if (newPos == default || ctx.Selection.SceneProxy is not { } proxy) return;

        var property = proxy.Properties.SpatialProperty;
        property.Transform.Translation = newPos;
        property.InvokeSet();
    }
}