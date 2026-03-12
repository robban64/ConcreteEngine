using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Numerics;

public record struct Ray(in Vector3 Position, in Vector3 Direction)
{
    public Vector3 Position = Position;
    public Vector3 Direction = Direction;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IntersectsWith(in BoundingBox bounds, out float distance) =>
        CollisionMethods.RayIntersectsBox(in this, in bounds, out distance);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector3 GetPointOnRay(float distance) => Position + Direction * distance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FromTwoPoints(in Vector3 p1, in Vector3 p2, out Ray ray) =>
        ray = new Ray(in p1, Vector3.Normalize(p2 - p1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetRayPlaneIntersectPoint(in Ray ray, float planeY)
    {
        float denom = ray.Direction.Y;
        if (float.Abs(denom) < 1e-6f) return default;
        float t = (planeY - ray.Position.Y) / denom;

        return t < 0 ? default : ray.GetPointOnRay(t);
    }

}