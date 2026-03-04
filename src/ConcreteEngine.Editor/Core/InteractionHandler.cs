using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;

namespace ConcreteEngine.Editor.Core;

internal sealed class InteractionHandler(InteractionController interaction, StateContext ctx)
{
    private InteractionMouseState _mouseState;

    public void Update()
    {
        _mouseState.MousePos = ImGui.GetMousePos();

        if (ImGui.IsKeyDown(ImGuiKey.Escape))
        {
            _mouseState.ResetState();
            _mouseState.PrevMousePos = _mouseState.MousePos;
            return;
        }

        if (ctx.SelectedSceneObject is { } inspector)
        {
            var gizmoEnable = _mouseState.DragState == DragState.None && !ImGui.IsItemHovered();
            ImGuizmo.Enable(gizmoEnable);
            EditorInputState.DrawGizmos(inspector);
        }

        if (!EditorInputState.IsBlockingViewport() && !UpdateMouseClick(EditorInputState.InputStateToggles))
            UpdateDrag(EditorInputState.InputStateToggles.IsDragging, ref _mouseState);

        _mouseState.WasDragging = EditorInputState.InputStateToggles.IsDragging;
        _mouseState.PrevMousePos = _mouseState.MousePos;
    }



    private bool UpdateMouseClick(InputStateToggles inputStateToggles)
    {
        if (inputStateToggles.IsRightClick)
        {
            OnRightClickViewport();
            return true;
        }

        if (inputStateToggles.IsUsingGizmo) return true;
        if (inputStateToggles is { IsLeftClick: true, IsDragging: false })
        {
            OnClickViewport(_mouseState.MousePos);
            return true;
        }

        return false;
    }

    private void UpdateDrag(bool isDragging, scoped ref InteractionMouseState mouseState)
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