#region

using System.Numerics;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Common.Numerics.Extensions;

public static class VectorExtensions
{
    public static Vector2I ToVec2Int(this Vector2D<int> v) => new(v.X, v.Y);

    public static Vector2 ToVec2(this Vector3 v) => new(v.X, v.Y);

    public static Vector3 ToVec3(this Vector2 v, float z = 0) => new(v.X, v.Y, z);

}