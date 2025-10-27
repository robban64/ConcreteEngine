#region

using System.Numerics;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Common.Numerics.Extensions;

public static class VectorExtensions
{
    public static Vector2I ToVector2I(this Vector2D<int> v) => new(v.X, v.Y);

    public static Vector2 ToVec2(this Vector3 v) => new(v.X, v.Y);

    public static Vector3 ToVec3(this Vector2 v, float z = 0) => new(v.X, v.Y, z);

    public static Vector2 ConvertToVec2(this Vector2D<int> v) => new(v.X, v.Y);

    public static Vector4D<float> ToSilkVec4(this Vector4 v) => new(v.X, v.Y, v.Z, v.W);
}