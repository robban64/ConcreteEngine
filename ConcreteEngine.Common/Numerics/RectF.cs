#region

using System.Numerics;

#endregion

namespace ConcreteEngine.Common.Numerics;

public struct RectF
{
    public float Left;
    public float Top;
    public float Width;
    public float Height;

    public readonly float Right => Left + Width;

    public readonly float Bottom => Top + Height;


    public RectF(float left, float top, float width, float height)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    public RectF(Vector4 vec)
    {
        Left = vec.X;
        Top = vec.Y;
        Width = vec.Z;
        Height = vec.W;
    }

    public readonly bool Contains(Vector2 point) =>
        point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
}