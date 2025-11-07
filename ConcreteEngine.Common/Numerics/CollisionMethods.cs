#region

using System.Numerics;
using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Common.Numerics;

internal sealed class CollisionMethods
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IntersectsBox(in BoundingBox box1, in BoundingBox box2)
    {
        if (box1.Min.X > box2.Max.X || box2.Min.X > box1.Max.X) return false;
        if (box1.Min.Y > box2.Max.Y || box2.Min.Y > box1.Max.Y) return false;
        if (box1.Min.Z > box2.Max.Z || box2.Min.Z > box1.Max.Z) return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool BoxInFrontOfPlane(in BoundingBox box, in Plane plane)
    {
        return Vector3.Dot(box.Center, plane.Normal)
               + box.Extent.X * MathF.Abs(plane.Normal.X)
               + box.Extent.Y * MathF.Abs(plane.Normal.Y)
               + box.Extent.Z * MathF.Abs(plane.Normal.Z)
               <= -plane.D;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 IntersectPlanes(in Plane p1, in Plane p2, in Plane p3)
    {
        var n1 = p1.Normal;
        var n2 = p2.Normal;
        var n3 = p3.Normal;

        var n2xn3 = Vector3.Cross(n2, n3);
        var n3xn1 = Vector3.Cross(n3, n1);
        var n1xn2 = Vector3.Cross(n1, n2);

        var denom = Vector3.Dot(n1, n2xn3);
        return -(p1.D * n2xn3 + p2.D * n3xn1 + p3.D * n1xn2) / denom;
    }
}