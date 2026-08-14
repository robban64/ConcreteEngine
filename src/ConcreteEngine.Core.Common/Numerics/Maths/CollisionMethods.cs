using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Numerics.Maths;

public static class CollisionMethods
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
        => RayIntersectsBox(in ray, box.Min, box.Max, out t);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RayIntersectsBox(in Ray ray, Vector3 min, Vector3 max, out float t)
    {
        var dirfrac = new Vector3
        {
            X = 1.0f / ray.Direction.X, Y = 1.0f / ray.Direction.Y, Z = 1.0f / ray.Direction.Z
        };
        float t1 = (min.X - ray.Position.X) * dirfrac.X;
        float t2 = (max.X - ray.Position.X) * dirfrac.X;
        float t3 = (min.Y - ray.Position.Y) * dirfrac.Y;
        float t4 = (max.Y - ray.Position.Y) * dirfrac.Y;
        float t5 = (min.Z - ray.Position.Z) * dirfrac.Z;
        float t6 = (max.Z - ray.Position.Z) * dirfrac.Z;

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


    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOutsidePlane(Vector4 center4, Vector4 extent4, ref Vector4 plane)
    {
        float d1 = Vector4.Dot(center4, plane);
        float d2 = Vector4.Dot(extent4, Vector4.Abs(plane));
        return d1 + d2 <= 0f;
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