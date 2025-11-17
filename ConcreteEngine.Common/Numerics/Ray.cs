#region

using System.Numerics;

#endregion

namespace ConcreteEngine.Common.Numerics;

public readonly record struct Ray(in Vector3 Position, in Vector3 Direction)
{
    public readonly Vector3 Position = Position;
    public readonly Vector3 Direction = Direction;

    public Vector3 GetPointOnRay(float distance) => Position + Direction * distance;

    public void Deconstruct(out Vector3 position, out Vector3 direction)
    {
        position = Position;
        direction = Direction;
    }

    public static Ray FromTwoPoints(in Vector3 p1, in Vector3 p2) => new(in p1, Vector3.Normalize(p2 - p1));
}   