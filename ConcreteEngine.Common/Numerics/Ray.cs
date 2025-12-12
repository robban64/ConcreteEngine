#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics.Maths;

#endregion

namespace ConcreteEngine.Common.Numerics;

public record struct Ray(in Vector3 Position, in Vector3 Direction)
{
    public Vector3 Position = Position;
    public Vector3 Direction = Direction;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IntersectsWith(in BoundingBox bounds, out float distance) =>
        CollisionMethods.RayIntersectsBox(in this, in bounds, out distance);


    public readonly Vector3 GetPointOnRay(float distance) => Position + Direction * distance;


    public static void FromTwoPoints(in Vector3 p1, in Vector3 p2, out Ray ray) =>
        ray = new Ray(in p1, Vector3.Normalize(p2 - p1));
}