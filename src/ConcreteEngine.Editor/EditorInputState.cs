using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;

namespace ConcreteEngine.Editor;

internal static class EditorInputState
{
    public static InputStateToggles InputStateToggles;
    
    public static ImGuizmoMode GizmoMode = ImGuizmoMode.World;
    public static ImGuizmoOperation GizmoOperation = ImGuizmoOperation.Translate;

    public static bool IsInteracting() =>
        InputStateToggles.IsDragging || InputStateToggles.IsUsingGizmo || InputStateToggles.IsHoveringGizmo;

    public static bool IsBlockingViewport() =>
        InputStateToggles.HasActiveInput || InputStateToggles.IsUsingGizmo || InputStateToggles.IsHovering;

    public static bool UpdateInputState()
    {
        var io = ImGui.GetIO();
        ref var state = ref InputStateToggles;
        state.IsUsingGizmo = ImGuizmo.IsUsing();
        state.IsHoveringGizmo = ImGuizmo.IsOver();
        state.HasActiveInput = io.WantTextInput;

        state.IsHovering = ImGui.IsAnyItemHovered() && !state.IsHoveringGizmo;
        state.IsDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
        state.IsLeftClick = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        state.IsRightClick = ImGui.IsMouseClicked(ImGuiMouseButton.Right);
        return state.IsDragging || state.IsUsingGizmo || state.IsHoveringGizmo;
    }
    
    
    public static unsafe void DrawGizmos(InspectSceneObject inspector)
    {
        //var entity = inspector.SceneObject.GetRenderEntities()[0];

        Matrix4x4* matrices = stackalloc Matrix4x4[3];
        var view = &matrices[0];
        var proj = &matrices[1];
        var model = &matrices[2];

        *view = EngineObjects.Camera.GetRenderViewMatrix();
        *proj = EngineObjects.Camera.GetProjectionMatrix();
        MatrixMath.CreateModelMatrix(in inspector.SceneObject.GetTransform(), out *model);

        var changed = ImGuizmo.Manipulate(
            &view->M11,
            &proj->M11,
            GizmoOperation,
            GizmoMode,
            &model->M11
        );

        if (changed)
        {
            Transform.FromMatrix(in *model, out var transform);
            inspector.SceneObject.SetTransform(in transform);
        }
    }

    private static void CheckHotkeys()
    {
        if (ImGui.IsItemFocused()) return;
/*
        if (ImGui.IsKeyDown(ImGuiKey.Key1)) states.SetLeftSidebarState(LeftSidebarMode.Assets);
        else if (ImGui.IsKeyDown(ImGuiKey.Key2)) states.SetLeftSidebarState(LeftSidebarMode.Scene);
        else if (ImGui.IsKeyDown(ImGuiKey.Key3)) states.SetRightSidebarState(RightSidebarMode.Camera);
        else if (ImGui.IsKeyDown(ImGuiKey.Key4)) states.SetRightSidebarState(RightSidebarMode.Visuals);
        else if (ImGui.IsKeyDown(ImGuiKey.Key5)) states.SetRightSidebarState(RightSidebarMode.Sky);
        else if (ImGui.IsKeyDown(ImGuiKey.Key6)) states.SetRightSidebarState(RightSidebarMode.Terrain);
        */
    }
}