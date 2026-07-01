namespace ConcreteEngine.Editor.Core;

internal enum TransformGizmoOp : byte
{
    None = 0,
    Translate = 1,
    Rotate = 2,
    Scale = 3
}

internal enum DragState : byte
{
    None = 0,
    DragStart = 1,
    Dragging = 2,
    DragEnd = 3,
}

internal struct InputStateToggles
{
    public bool IsDragging;
    public bool IsLeftClick;
    public bool IsRightClick;

    public bool IsUsingGizmo;
    public bool IsHoveringGizmo;

    public bool IsHoveringUi;

    public bool IsBlockingKeyboard;
    public bool IsBlockingMouse;
}