using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Common.Numerics.Maths;

public sealed class CollisionMethods
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
    public static bool RayIntersectsBox(in Ray ray, in BoundingBox box, out float t)
    {
        var dirfrac = new Vector3
        {
            X = 1.0f / ray.Direction.X, Y = 1.0f / ray.Direction.Y, Z = 1.0f / ray.Direction.Z
        };
        float t1 = (box.Min.X - ray.Position.X) * dirfrac.X;
        float t2 = (box.Max.X - ray.Position.X) * dirfrac.X;
        float t3 = (box.Min.Y - ray.Position.Y) * dirfrac.Y;
        float t4 = (box.Max.Y - ray.Position.Y) * dirfrac.Y;
        float t5 = (box.Min.Z - ray.Position.Z) * dirfrac.Z;
        float t6 = (box.Max.Z - ray.Position.Z) * dirfrac.Z;

        float tmin = MathF.Max(MathF.Max(MathF.Min(t1, t2), MathF.Min(t3, t4)), MathF.Min(t5, t6));
        float tmax = MathF.Min(MathF.Min(MathF.Max(t1, t2), MathF.Max(t3, t4)), MathF.Max(t5, t6));

        // behind
        if (tmax < 0)
        {
            t = tmax;
            return false;
        }

        // miss
        if (tmin > tmax)
        {
            t = tmax;
            return false;
        }

        t = tmin;
        return true;
    }

    public static bool IntersectsPlane(in BoundingBox box, in Plane plane)
    {
        var ext = box.Extent;
        return Vector3.Dot(box.Center, plane.Normal)
               + ext.X * MathF.Abs(plane.Normal.X)
               + ext.Y * MathF.Abs(plane.Normal.Y)
               + ext.Z * MathF.Abs(plane.Normal.Z)
               <= -plane.D;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOutsidePlane(in BoundingBox box, in Plane plane)
    {
        var ext = box.Extent;
        return Vector3.Dot(plane.Normal, box.Center) + plane.D +
               ext.X * MathF.Abs(plane.Normal.X) +
               ext.Y * MathF.Abs(plane.Normal.Y) +
               ext.Z * MathF.Abs(plane.Normal.Z)
               < 0f;
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