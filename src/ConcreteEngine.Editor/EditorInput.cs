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

    public static DragState DragState;

    public static bool IsDragging;
    public static bool IsLeftClick;
    public static bool IsRightClick;

    public static bool IsHoveringUi;

    public static bool IsUsingGizmo;
    public static bool IsHoveringGizmo;

    public static bool IsBlockingKeyboard;
    public static bool IsBlockingMouse;
    
    public static bool IsBlocking => IsBlockingMouse || IsBlockingKeyboard;
    public static bool IsInteracting() => IsDragging || IsUsingGizmo || IsHoveringGizmo;
    public static bool IsGizmoBlocked => DragState != DragState.None || Layer.IsKeyDown(Key.ControlLeft);

    public static void ToggleBlockLayers()
    {
        if (IsBlocking) EngineInput.SetActiveLayer(InputLayerKind.Ui);
        else EngineInput.ActiveAllLayers();
    }

    public static bool UpdateInputState(bool hasGizmo)
    {
        IsLeftClick = Layer.IsMouseClicked(MouseButton.Left);
        IsRightClick = Layer.IsMouseClicked(MouseButton.Right);
        var isDragging = IsDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);

        var isUsingGizmo = hasGizmo && ImGuizmo.IsUsing();
        var isIsHoveringGizmo = hasGizmo && ImGuizmo.IsOver();
        var isHovering = !ViewportWindow.IsHovering && ImGuiSystem.Io.WantCaptureMouse && !isUsingGizmo;
        
        IsUsingGizmo = isUsingGizmo;
        IsHoveringGizmo = isIsHoveringGizmo;
        IsHoveringUi = isHovering;
        IsBlockingKeyboard = IsBlockingMouse = ImGuiSystem.Io.WantTextInput || isUsingGizmo ||
                                               (isHovering && !isIsHoveringGizmo);
        
        return isDragging || isUsingGizmo || isIsHoveringGizmo;
    }
}