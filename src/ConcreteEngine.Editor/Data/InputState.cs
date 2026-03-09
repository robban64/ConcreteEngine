using System.Numerics;

namespace ConcreteEngine.Editor.Data;

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

    public bool IsBlockingKeyboard;
    public bool IsBlockingMouse;

}

internal struct InteractionMouseState
{
    public Vector3 DragStart;
    public Vector2 MousePos;
    public Vector2 PrevMousePos;

    public DragState DragState;
    public bool WasDragging;

    public void ResetState()
    {
        WasDragging = false;
        DragState = DragState.None;
        DragStart = Vector3.Zero;
    }
}