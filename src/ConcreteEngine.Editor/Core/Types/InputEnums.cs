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
