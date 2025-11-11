#region

using System.Numerics;
using System.Runtime.CompilerServices;
using static ConcreteEngine.Common.Numerics.Maths.CollisionMethods;

#endregion

namespace ConcreteEngine.Common.Numerics;

public struct BoundingFrustum
{
    public Plane LeftPlane;
    public Plane RightPlane;
    public Plane TopPlane;
    public Plane BottomPlane;
    public Plane NearPlane;
    public Plane FarPlane;

    public BoundingFrustum(in Matrix4x4 matrix)
    {
        LeftPlane = Plane.Normalize(
            new Plane(matrix.M14 + matrix.M11,
                matrix.M24 + matrix.M21,
                matrix.M34 + matrix.M31,
                matrix.M44 + matrix.M41)
        );

        RightPlane = Plane.Normalize(
            new Plane(matrix.M14 - matrix.M11,
                matrix.M24 - matrix.M21,
                matrix.M34 - matrix.M31,
                matrix.M44 - matrix.M41)
        );

        TopPlane = Plane.Normalize(
            new Plane(matrix.M14 - matrix.M12,
                matrix.M24 - matrix.M22,
                matrix.M34 - matrix.M32,
                matrix.M44 - matrix.M42)
        );

        BottomPlane = Plane.Normalize(
            new Plane(matrix.M14 + matrix.M12,
                matrix.M24 + matrix.M22,
                matrix.M34 + matrix.M32,
                matrix.M44 + matrix.M42)
        );

        NearPlane = Plane.Normalize(new Plane(matrix.M13, matrix.M23, matrix.M33, matrix.M43));

        FarPlane = Plane.Normalize(
            new Plane(matrix.M14 - matrix.M13,
                matrix.M24 - matrix.M23,
                matrix.M34 - matrix.M33,
                matrix.M44 - matrix.M43)
        );
    }

    public BoundingFrustum(ReadOnlySpan<Vector3> corners)
    {
        NearPlane = Plane.Normalize(PlaneFromPoints(corners[0], corners[1], corners[2]));
        FarPlane = Plane.Normalize(PlaneFromPoints(corners[4], corners[6], corners[5]));
        LeftPlane = Plane.Normalize(PlaneFromPoints(corners[0], corners[2], corners[4]));
        RightPlane = Plane.Normalize(PlaneFromPoints(corners[1], corners[5], corners[3]));
        TopPlane = Plane.Normalize(PlaneFromPoints(corners[0], corners[4], corners[1]));
        BottomPlane = Plane.Normalize(PlaneFromPoints(corners[2], corners[3], corners[6]));
    }
    
    public readonly bool ContainsBox(in BoundingBox box)
    {
        return BoxInFrontOfPlane(in box, in LeftPlane)
               && BoxInFrontOfPlane(in box, in RightPlane)
               && BoxInFrontOfPlane(in box, in TopPlane)
               && BoxInFrontOfPlane(in box, in BottomPlane)
               && BoxInFrontOfPlane(in box, in NearPlane)
               && BoxInFrontOfPlane(in box, in FarPlane);
    }

    public readonly void GetCorners(Span<Vector3> corners)
    {
        corners[0] = IntersectPlanes(NearPlane, TopPlane, LeftPlane);
        corners[1] = IntersectPlanes(NearPlane, TopPlane, RightPlane);
        corners[2] = IntersectPlanes(NearPlane, BottomPlane, LeftPlane);
        corners[3] = IntersectPlanes(NearPlane, BottomPlane, RightPlane);
        corners[4] = IntersectPlanes(FarPlane, TopPlane, LeftPlane);
        corners[5] = IntersectPlanes(FarPlane, TopPlane, RightPlane);
        corners[6] = IntersectPlanes(FarPlane, BottomPlane, LeftPlane);
        corners[7] = IntersectPlanes(FarPlane, BottomPlane, RightPlane);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Plane PlaneFromPoints(Vector3 a, Vector3 b, Vector3 c)
    {
        var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
        float d = -Vector3.Dot(normal, a);
        return new Plane(normal, d);
    }


}