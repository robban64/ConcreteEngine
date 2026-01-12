using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Engine.Input;

public record struct InputButtonState
{
    public bool Down;
    public bool Up;
    public bool WasDown;
    public bool Pressed;

    public readonly bool IsHeld
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Down && !Pressed;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        Pressed = Down && !WasDown;
        WasDown = Down;
        if (Up) Down = false;
    }
}

public record struct InputMouseState
{
    public Vector2 Position;
    public Vector2 Scroll;
    public Vector2 Delta;
}