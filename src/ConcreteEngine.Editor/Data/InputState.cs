using System.Numerics;

namespace ConcreteEngine.Editor.Data;

internal enum DragState : byte
{
    None = 0,
    DragStart = 1,
    Dragging = 2,
    DragEnd = 3,
}

internal struct EditorInputState
{
    public bool HasActiveInput;
    public bool HasActiveMouse;
    public bool IsInteracting;

    public bool IsDragging;
    public bool IsLeftClick;
    public bool IsRightClick;
}

internal struct EditorMouseState
{
    public Vector3 DragStart;
    public Vector2 MousePos;
    public Vector2 PrevMousePos;

    public DragState DragState;
    public bool WasDragging;
}