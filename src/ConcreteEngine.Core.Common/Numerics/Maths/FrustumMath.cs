using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Numerics.Maths;

public static class FrustumMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetFrustumCenter(Span<Vector3> corners)
    {
        Vector3 s = default;
        foreach (ref readonly var c in corners) s += c;
        return s / corners.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillFrustumCorners(in Matrix4x4 viewMat, Vector3 translation, Vector2 tan, Vector2 nearFar,
        Span<Vector3> corners)
    {
        var right = new Vector3(viewMat.M11, viewMat.M21, viewMat.M31);
        var up = new Vector3(viewMat.M12, viewMat.M22, viewMat.M32);
        var forward = -new Vector3(viewMat.M13, viewMat.M23, viewMat.M33);

        // extents at near/far
        float nx = nearFar.X * tan.X, ny = nearFar.X * tan.Y;
        float fx = nearFar.Y * tan.X, fy = nearFar.Y * tan.Y;

        var nc = translation + forward * nearFar.X;
        var fc = translation + forward * nearFar.Y;

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