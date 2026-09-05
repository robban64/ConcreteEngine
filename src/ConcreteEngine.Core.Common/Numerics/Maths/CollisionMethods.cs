using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace ConcreteEngine.Core.Common.Numerics.Maths;

public static class CollisionMethods
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IntersectsBox(in BoundingBox box1, in BoundingBox box2)
    {
        //var x = Vector128.GreaterThan(box1.Min.AsVector128(), box2.Max.AsVector128());
        //var y = Vector128.GreaterThan(box2.Min.AsVector128(), box1.Max.AsVector128());
        if (box1.Min.X > box2.Max.X || box2.Min.X > box1.Max.X) return false;
        if (box1.Min.Y > box2.Max.Y || box2.Min.Y > box1.Max.Y) return false;
        if (box1.Min.Z > box2.Max.Z || box2.Min.Z > box1.Max.Z) return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RayIntersectsBox(in Ray ray, in BoundingBox box, out float t)
        => RayIntersectsBox(in ray, box.Min, box.Max, out t);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RayIntersectsBox(in Ray ray, Vector3 min, Vector3 max, out float t)
    {
        float px = ray.Position.X, py = ray.Position.Y, pz = ray.Position.Z;
        var dirFrac = new Vector3
        {
            X = 1.0f / ray.Direction.X, Y = 1.0f / ray.Direction.Y, Z = 1.0f / ray.Direction.Z
        };

        float t1 = (min.X - px) * dirFrac.X;
        float t2 = (max.X - px) * dirFrac.X;
        float t3 = (min.Y - py) * dirFrac.Y;
        float t4 = (max.Y - py) * dirFrac.Y;
        float t5 = (min.Z - pz) * dirFrac.Z;
        float t6 = (max.Z - pz) * dirFrac.Z;

        float tMin = MathF.Max(MathF.Max(MathF.Min(t1, t2), MathF.Min(t3, t4)), MathF.Min(t5, t6));
        float tMax = MathF.Min(MathF.Min(MathF.Max(t1, t2), MathF.Max(t3, t4)), MathF.Max(t5, t6));

        // behind
        if (tMax < 0)
        {
            t = tMax;
            return false;
        }

        // miss
        if (tMin > tMax)
        {
            t = tMax;
            return false;
        }

        t = tMin;
        return true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOutsidePlane(Vector4 center4, Vector4 extent4, in Vector4 plane)
    {
        var d1 = Vector256.Create(center4.AsVector128(), extent4.AsVector128());
        var d2 = Vector256.Create(plane.AsVector128(), Vector128.Abs(plane.AsVector128()));
        return Vector256.Dot(d1, d2) <= 0f;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 IntersectPlanes(in Plane p1, in Plane p2, in Plane p3)
    {
        var n1 = p1.Normal;
        var n2 = p2.Normal;
        var n3 = p3.Normal;
        var d1 = p1.D;
        var d2 = p2.D;
        var d3 = p3.D;

        var n2xn3 = Vector3.Cross(n2, n3);
        var n3xn1 = Vector3.Cross(n3, n1);
        var n1xn2 = Vector3.Cross(n1, n2);

        var denom = Vector3.Dot(n1, n2xn3);
        return -(d1 * n2xn3 + d2 * n3xn1 + d3 * n1xn2) / denom;
    }
}