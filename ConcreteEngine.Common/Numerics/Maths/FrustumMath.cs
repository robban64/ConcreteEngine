#region

using System.Numerics;
using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Common.Numerics.Maths;

public static class FrustumMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetFrustumCenter(Span<Vector3> corners)
    {
        Vector3 s = default;
        foreach (var c in corners) s += c;
        return s / corners.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillFrustumCorners(
        in Matrix4x4 viewMat,
        in Matrix4x4 projMat,
        Vector3 pos,
        Vector2 nearFar,
        Span<Vector3> corners)
    {
        var tanY = 1f / projMat.M22;
        var tanX = 1f / projMat.M11;

        var right = new Vector3(viewMat.M11, viewMat.M21, viewMat.M31);
        var up = new Vector3(viewMat.M12, viewMat.M22, viewMat.M32);
        var forward = -new Vector3(viewMat.M13, viewMat.M23, viewMat.M33);

        // extents at near/far
        float nx = nearFar.X * tanX, ny = nearFar.X * tanY;
        float fx = nearFar.Y * tanX, fy = nearFar.Y * tanY;

        var nc = pos + forward * nearFar.X;
        var fc = pos + forward * nearFar.Y;

        // NearPlane plane
        corners[0] = nc + up * ny - right * nx; // NT-L
        corners[1] = nc + up * ny + right * nx; // NT-R
        corners[2] = nc - up * ny - right * nx; // NB-L
        corners[3] = nc - up * ny + right * nx; // NB-R

        // FarPlane plane
        corners[4] = fc + up * fy - right * fx; // FT-L
        corners[5] = fc + up * fy + right * fx; // FT-R
        corners[6] = fc - up * fy - right * fx; // FB-L
        corners[7] = fc - up * fy + right * fx; // FB-R
    }
}