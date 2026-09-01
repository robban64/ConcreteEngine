using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Numerics;

public struct Ray(Vector3 position, Vector3 direction) : IEquatable<Ray>
{
    public Vector3 Position = position;
    public Vector3 Direction = direction;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector3 GetPointOnRay(float distance) => Position + Direction * distance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FromTwoPoints(Vector3 p1, Vector3 p2, out Ray ray)
    {
        ray.Position = p1;
        ray.Direction = Vector3.Normalize(p2 - p1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetRayPlaneIntersectPoint(in Ray ray, float planeY)
    {
        float denom = ray.Direction.Y;
        if (float.Abs(denom) < 1e-6f) return default;
        
        float t = (planeY - ray.Position.Y) / denom;
        return t < 0 ? default : ray.GetPointOnRay(t);
    }

    public readonly bool Equals(Ray other) => Position == other.Position && Direction == other.Direction;

    public override readonly bool Equals(object? obj) => obj is Ray other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(Position, Direction);
}