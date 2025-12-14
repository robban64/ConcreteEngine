using System.Numerics;

namespace ConcreteEngine.Common.Numerics.Maths;

public static class CoordinateMath
{
    public static Vector2 ToUvCoords(Vector2 v, Size2D outputSize) =>
        new(v.X / outputSize.Width, 1.0f - v.Y / outputSize.Height);

    public static Vector2 ToNdcCoords(Vector2 v, Size2D outputSize)
    {
        var ndcX = 2.0f * v.X / outputSize.Width - 1.0f;
        var ndcY = 1.0f - 2.0f * v.Y / outputSize.Height;
        return new Vector2(ndcX, ndcY);
    }
}