using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.Platform;

internal record struct ButtonState
{
    public bool Down;
    public bool Up;
    public bool WasDown;
    public bool Pressed;
    
    public readonly bool IsDown => Down && !Pressed;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        Pressed = Down && !WasDown;
        WasDown = Down;
        if (Up) Down = false;
    }
}

public record struct MouseStateSnapshot
{
    public Vector2 MousePosition;
    public Vector2 Scroll;
    public Vector2 MouseDelta;

    public MouseStateSnapshot(Vector2 mousePosition, Vector2 mouseDelta, Vector2 scroll)
    {
        MousePosition = mousePosition;
        MouseDelta = mouseDelta;
        Scroll = scroll;
    }
}