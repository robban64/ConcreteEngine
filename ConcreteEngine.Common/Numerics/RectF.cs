using System.Numerics;

namespace ConcreteEngine.Common.Numerics;

public readonly record struct RectF(float Left, float Top, float Width, float Height)
{
    public readonly float Left = Left;
    public readonly float Top = Top;
    public readonly float Width = Width;
    public readonly float Height = Height;

    public float Right => Left + Width;
    
    public float Bottom => Top + Height;

    public RectF(Vector4 vec) : this(vec.X, vec.Y, vec.Z, vec.W)
    {
        
    }

    public bool Contains(Vector2 point) =>
        point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
}
