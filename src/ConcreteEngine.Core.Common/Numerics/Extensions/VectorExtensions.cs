using Silk.NET.Maths;

namespace ConcreteEngine.Core.Common.Numerics.Extensions;

public static class VectorExtensions
{
    public static Int2 ToVec2Int(this Vector2D<int> v) => new(v.X, v.Y);
}