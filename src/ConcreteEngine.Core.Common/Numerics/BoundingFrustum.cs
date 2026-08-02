using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static ConcreteEngine.Core.Common.Numerics.Maths.CollisionMethods;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct BoundingFrustum
{
    public Plane LeftPlane;
    public Plane RightPlane;
    public Plane TopPlane;
    public Plane BottomPlane;
    public Plane NearPlane;
    public Plane FarPlane;
    
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
    public static void From(in Matrix4x4 transposedViewProjection, out BoundingFrustum frustum)
    {
        ref Vector4 cols = ref Unsafe.As<Matrix4x4, Vector4>(ref Unsafe.AsRef(in transposedViewProjection));
        Vector4 col1 = Unsafe.Add(ref cols, 0);
        Vector4 col2 = Unsafe.Add(ref cols, 1);
        Vector4 col3 = Unsafe.Add(ref cols, 2);
        Vector4 col4 = Unsafe.Add(ref cols, 3);

        frustum.LeftPlane = NormalizePlane(col4 + col1);
        frustum.RightPlane = NormalizePlane(col4 - col1);
        frustum.TopPlane = NormalizePlane(col4 - col2);
        frustum.BottomPlane = NormalizePlane(col4 + col2);
        frustum.NearPlane = NormalizePlane(col4 + col3);
        frustum.FarPlane = NormalizePlane(col4 - col3);
    }
    
    public static void FromCorners(ReadOnlySpan<Vector3> corners, out BoundingFrustum frustum)
    {
        frustum.LeftPlane = Plane.Normalize(PlaneFromPoints(corners[0], corners[2], corners[4]));
        frustum.RightPlane = Plane.Normalize(PlaneFromPoints(corners[1], corners[5], corners[3]));
        frustum.TopPlane = Plane.Normalize(PlaneFromPoints(corners[0], corners[4], corners[1]));
        frustum.BottomPlane = Plane.Normalize(PlaneFromPoints(corners[2], corners[3], corners[6]));
        frustum.NearPlane = Plane.Normalize(PlaneFromPoints(corners[0], corners[1], corners[2]));
        frustum. FarPlane = Plane.Normalize(PlaneFromPoints(corners[4], corners[6], corners[5]));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Plane PlaneFromPoints(Vector3 a, Vector3 b, Vector3 c)
    {
        var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
        float d = -Vector3.Dot(normal, a);
        return new Plane(normal, d);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Plane NormalizePlane(float x, float y, float z, float d)
    {
        float lengthSq = x * x + y * y + z * z;
        float invLength = 1.0f / MathF.Sqrt(lengthSq);
        return new Plane(x * invLength, y * invLength, z * invLength, d * invLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Plane NormalizePlane(Vector4 p)
    {
        float lengthSq = p.LengthSquared();
        float invLength = 1f / MathF.Sqrt(lengthSq);
        Vector4 normalized = p * invLength;
        return Unsafe.As<Vector4, Plane>(ref normalized);
    }


}