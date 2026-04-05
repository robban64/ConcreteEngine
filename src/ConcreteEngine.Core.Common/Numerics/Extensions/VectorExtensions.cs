using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Common.Numerics.Extensions;

public static class VectorExtensions
{
    public static Vector2I ToVec2Int(this Vector2D<int> v) => new(v.X, v.Y);
}