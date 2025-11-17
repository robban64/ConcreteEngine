using System.Numerics;

namespace ConcreteEngine.Common.Numerics.Maths;

public sealed class CoordinateMath
{
    public static Vector2 ToUvCoords(Vector2 v, Size2D outputSize) =>
        new(v.X / outputSize.Width, 1.0f - (v.Y / outputSize.Height));
}