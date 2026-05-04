using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Input;

public sealed class MouseState
{
    public Vector2 ScreenPos { get; private set; }
    public Vector2 ViewPos { get; private set; }
    public Vector2 Scroll { get; private set; }
    public Vector2 Delta { get; private set; }

    internal void Set(Vector2 screenPos, Vector2 scroll, Vector2 delta, Vector2I viewportPos)
    {
        ScreenPos = screenPos;
        ViewPos = screenPos - viewportPos;
        Scroll = scroll;
        Delta = delta;
    }
    
}