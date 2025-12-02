#region

using System.Numerics;

#endregion

namespace ConcreteEngine.Common.Numerics;

public struct RectF(float left, float top, float width, float height)
{
    public float Left = left;
    public float Top = top;
    public float Width = width;
    public float Height = height;

    public readonly float Right => Left + Width;

    public readonly float Bottom => Top + Height;

    public RectF(Vector4 vec) : this(vec.X, vec.Y, vec.Z, vec.W)
    {
    }

    public readonly bool Contains(Vector2 point) => point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
}