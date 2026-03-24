using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Silk.NET.Input;

namespace ConcreteEngine.Editor;

internal static class EditorInputState
{
    private const ImGuiHoveredFlags HoveringFlags = ImGuiHoveredFlags.AnyWindow |
                                                    ImGuiHoveredFlags.AllowWhenBlockedByPopup |
                                                    ImGuiHoveredFlags.AllowWhenBlockedByActiveItem;

    public static InputController Input = null!;
    public static InputStateToggles InputStateToggles;

    public static ImGuizmoMode GizmoMode = ImGuizmoMode.World;
    public static ImGuizmoOperation GizmoOperation = ImGuizmoOperation.Translate;

    public static bool IsInteracting() =>
        InputStateToggles.IsDragging || InputStateToggles.IsUsingGizmo || InputStateToggles.IsHoveringGizmo;

    public static bool IsBlockingMouse => InputStateToggles.IsBlockingMouse;

    public static bool IsBlockingKeyboard => InputStateToggles.IsBlockingKeyboard;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateInputBlock() => Input.ToggleBlockInput(IsBlockingKeyboard || IsBlockingMouse);

    public static bool UpdateInputState()
    {
        var io = ImGuiSystem.Io;
        ref var state = ref InputStateToggles;
        state.IsDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
        state.IsLeftClick = Input.IsMouseDown(MouseButton.Left);
        state.IsRightClick = Input.IsMouseDown(MouseButton.Right);

        state.IsUsingGizmo = ImGuizmo.IsUsing();
        state.IsHoveringGizmo = ImGuizmo.IsOver();

        state.IsHoveringUi = ImGui.IsWindowHovered(HoveringFlags) && !state.IsUsingGizmo;

        state.IsBlockingKeyboard = io.WantTextInput || state.IsUsingGizmo ||
                                   (state.IsHoveringUi && !state.IsHoveringGizmo);

        state.IsBlockingMouse = io.WantTextInput || state.IsUsingGizmo ||
                                (io.WantCaptureMouse && !state.IsHoveringGizmo);

        return state.IsDragging || state.IsUsingGizmo || state.IsHoveringGizmo;
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