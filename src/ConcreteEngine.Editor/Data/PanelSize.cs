using System.Numerics;

namespace ConcreteEngine.Editor.Data;

public struct PanelSize
{
    public Vector2 LeftPosition;
    public Vector2 LeftSize;
    public Vector2 RightPosition;
    public Vector2 RightSize;
}

public struct ConsoleWindowSize
{
    public Vector2 Position;
    public Vector2 Size;
    public Vector2 SizeConstraintMin;
    public Vector2 SizeConstraintMax;
}