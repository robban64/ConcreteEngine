using System.Numerics;

namespace ConcreteEngine.Shared.Input;

public struct MouseDataState
{
    public Vector2 Position;
    public Vector2 Delta;
    public float ScrollDelta;

    public bool LeftDown;
    public bool RightDown;
    public bool MiddleDown;

    public void SetPosition(Vector2 position, Vector2 delta)
    {
        Position = position;
        Delta = delta;
    }
}