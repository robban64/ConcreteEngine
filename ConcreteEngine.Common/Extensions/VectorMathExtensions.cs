#region

using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Common.Extensions;

public static class VectorMathExtensions
{
    public static Vector2D<T> ToVec2<T>(this Vector3D<T> v)
        where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T> => new(v.X, v.Y);

    public static Vector3D<T> ToVec3<T>(this Vector2D<T> v, T z = default)
        where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T> => new(v.X, v.Y, z);
}