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

    public BoundingFrustum(in Matrix4x4 viewProj)
    {
        LeftPlane = NormalizePlane(
            viewProj.M14 + viewProj.M11,
            viewProj.M24 + viewProj.M21,
            viewProj.M34 + viewProj.M31,
            viewProj.M44 + viewProj.M41);

        RightPlane = NormalizePlane(
            viewProj.M14 - viewProj.M11,
            viewProj.M24 - viewProj.M21,
            viewProj.M34 - viewProj.M31,
            viewProj.M44 - viewProj.M41);

        TopPlane = NormalizePlane(
            viewProj.M14 - viewProj.M12,
            viewProj.M24 - viewProj.M22,
            viewProj.M34 - viewProj.M32,
            viewProj.M44 - viewProj.M42);

        BottomPlane = NormalizePlane(
            viewProj.M14 + viewProj.M12,
            viewProj.M24 + viewProj.M22,
            viewProj.M34 + viewProj.M32,
            viewProj.M44 + viewProj.M42);

        NearPlane = NormalizePlane(
            viewProj.M14 + viewProj.M13,
            viewProj.M24 + viewProj.M23,
            viewProj.M34 + viewProj.M33,
            viewProj.M44 + viewProj.M43);

        FarPlane = NormalizePlane(
            viewProj.M14 - viewProj.M13,
            viewProj.M24 - viewProj.M23,
            viewProj.M34 - viewProj.M33,
            viewProj.M44 - viewProj.M43);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IntersectsBox(in BoundingBox box)
    {
        return !IsOutsidePlane(in box, in LeftPlane) &&
               !IsOutsidePlane(in box, in RightPlane) &&
               !IsOutsidePlane(in box, in TopPlane) &&
               !IsOutsidePlane(in box, in BottomPlane) &&
               !IsOutsidePlane(in box, in NearPlane) &&
               !IsOutsidePlane(in box, in FarPlane);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Plane NormalizePlane(float x, float y, float z, float d)
    {
        float lengthSq = x * x + y * y + z * z;
        float invLength = 1.0f / MathF.Sqrt(lengthSq);
        return new Plane(x * invLength, y * invLength, z * invLength, d * invLength);
    }
}