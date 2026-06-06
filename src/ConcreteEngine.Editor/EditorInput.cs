using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Silk.NET.Input;

namespace ConcreteEngine.Editor;

internal static class EditorInput
{
    private const ImGuiHoveredFlags HoveringFlags = //ImGuiHoveredFlags.AnyWindow |
        ImGuiHoveredFlags.AllowWhenBlockedByPopup |
        ImGuiHoveredFlags.AllowWhenBlockedByActiveItem;

    public static InputLayer Layer = null!;

    public static InputStateToggles State;

    public static DragState DragState;

    public static bool IsInteracting() => State.IsDragging || State.IsUsingGizmo || State.IsHoveringGizmo;

    public static bool IsBlockingMouse => State.IsBlockingMouse;
    public static bool IsBlockingKeyboard => State.IsBlockingKeyboard;
    public static bool IsBlocking => State.IsBlockingMouse || State.IsBlockingKeyboard;

    public static bool IsGizmoBlocked => DragState != DragState.None || Layer.IsKeyDown(Key.ControlLeft);
    
    public static void ToggleBlockLayers()
    {
        if (IsBlocking) EngineInput.SetActiveLayer(InputLayerKind.Ui);
        else EngineInput.ActiveAllLayers();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool UpdateInputState()
    {
        var io = ImGuiSystem.Io;
        ref var state = ref State;
        state.IsDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
        state.IsLeftClick = Layer.IsMouseDown(MouseButton.Left);
        state.IsRightClick = Layer.IsMouseDown(MouseButton.Right);

        state.IsUsingGizmo = ImGuizmo.IsUsing();
        state.IsHoveringGizmo = ImGuizmo.IsOver();

        //state.IsHoveringUi = ImGui.IsWindowHovered(HoveringFlags) && !state.IsUsingGizmo;
        state.IsHoveringUi = !ViewportWindow.IsHovering && io.WantCaptureMouse && !state.IsUsingGizmo;

        state.IsBlockingKeyboard = state.IsBlockingMouse = io.WantTextInput || state.IsUsingGizmo ||
                                                           (state.IsHoveringUi && !state.IsHoveringGizmo);

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