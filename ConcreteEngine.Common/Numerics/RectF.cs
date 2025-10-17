#region

using System.Numerics;

#endregion

namespace ConcreteEngine.Common.Numerics;

public readonly struct RectF(float left, float top, float width, float height)
{
    public readonly float Left = left;
    public readonly float Top = top;
    public readonly float Width = width;
    public readonly float Height = height;

    public float Right => Left + Width;

    public float Bottom => Top + Height;

    public RectF(Vector4 vec) : this(vec.X, vec.Y, vec.Z, vec.W)
    {
    }

    public bool Contains(Vector2 point) => point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

}