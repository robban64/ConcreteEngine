using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Editor.App;
using ConcreteEngine.Editor.Core;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Silk.NET.Input;

namespace ConcreteEngine.Editor;

internal static class EditorInput
{
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

    public static bool UpdateInputState(bool hasGizmo)
    {
        var isDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
        var isUsingGizmo = hasGizmo && ImGuizmo.IsUsing();
        var isIsHoveringGizmo = hasGizmo && ImGuizmo.IsOver();
        var isHovering = !ViewportWindow.IsHovering && ImGuiSystem.Io.WantCaptureMouse && !isUsingGizmo;

        ref var state = ref State;
        state.IsDragging = isDragging;
        state.IsLeftClick = Layer.IsMouseDown(MouseButton.Left);
        state.IsRightClick = Layer.IsMouseDown(MouseButton.Right);

        state.IsUsingGizmo = isUsingGizmo;
        state.IsHoveringGizmo = isIsHoveringGizmo;
        state.IsHoveringUi = isHovering;

        state.IsHoveringUi = isHovering;
        state.IsBlockingKeyboard = state.IsBlockingMouse = ImGuiSystem.Io.WantTextInput || isUsingGizmo ||
                                                           (isHovering && !isIsHoveringGizmo);

        return isDragging || isUsingGizmo || isIsHoveringGizmo;
    }

}