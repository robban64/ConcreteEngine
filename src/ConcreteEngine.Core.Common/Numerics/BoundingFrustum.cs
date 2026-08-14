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
    public static void From(in Matrix4x4 transposedViewProjection, out BoundingFrustum f)
    {
        ref Vector4 cols = ref Unsafe.As<Matrix4x4, Vector4>(ref Unsafe.AsRef(in transposedViewProjection));
        Vector4 col1 = Unsafe.Add(ref cols, 0);
        Vector4 col2 = Unsafe.Add(ref cols, 1);
        Vector4 col3 = Unsafe.Add(ref cols, 2);
        Vector4 col4 = Unsafe.Add(ref cols, 3);

        f.LeftPlane = NormalizePlane(col4 + col1);
        f.RightPlane = NormalizePlane(col4 - col1);
        f.TopPlane = NormalizePlane(col4 - col2);
        f.BottomPlane = NormalizePlane(col4 + col2);
        f.NearPlane = NormalizePlane(col4 + col3);
        f.FarPlane = NormalizePlane(col4 - col3);
    }
    
    public static void FromCorners(ReadOnlySpan<Vector3> corners, out BoundingFrustum f)
    {
        f.LeftPlane = Plane.Normalize(PlaneFromPoints(corners[0], corners[2], corners[4]));
        f.RightPlane = Plane.Normalize(PlaneFromPoints(corners[1], corners[5], corners[3]));
        f.TopPlane = Plane.Normalize(PlaneFromPoints(corners[0], corners[4], corners[1]));
        f.BottomPlane = Plane.Normalize(PlaneFromPoints(corners[2], corners[3], corners[6]));
        f.NearPlane = Plane.Normalize(PlaneFromPoints(corners[0], corners[1], corners[2]));
        f. FarPlane = Plane.Normalize(PlaneFromPoints(corners[4], corners[6], corners[5]));
    }

    public static void GetCorners(in BoundingFrustum f, Span<Vector3> corners)
    {
        corners[0] = IntersectPlanes(in f.NearPlane, in f.TopPlane, in f.LeftPlane);
        corners[1] = IntersectPlanes(in f.NearPlane, in f.TopPlane, in f.RightPlane);
        corners[2] = IntersectPlanes(in f.NearPlane, in f.BottomPlane, in f.LeftPlane);
        corners[3] = IntersectPlanes(in f.NearPlane, in f.BottomPlane, in f.RightPlane);
        corners[4] = IntersectPlanes(in f.FarPlane, in f.TopPlane, in f.LeftPlane);
        corners[5] = IntersectPlanes(in f.FarPlane, in f.TopPlane, in f.RightPlane);
        corners[6] = IntersectPlanes(in f.FarPlane, in f.BottomPlane, in f.LeftPlane);
        corners[7] = IntersectPlanes(in f.FarPlane, in f.BottomPlane, in f.RightPlane);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Plane PlaneFromPoints(Vector3 a, Vector3 b, Vector3 c)
    {
        var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
        var d = -Vector3.Dot(normal, a);
        return new Plane(normal, d);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Plane NormalizePlane(Vector4 p)
    {
        var lengthSq = p.LengthSquared();
        var invLength = 1.0f / MathF.Sqrt(lengthSq);
        Vector4 normalized = p * invLength;
        return Unsafe.As<Vector4, Plane>(ref normalized);
    }


}