#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Renderer.State;

#endregion

namespace ConcreteEngine.Renderer.Utility;

internal static class RenderTransform
{
    public static void CreateDirLightView(
        Vector3 lightDirection,
        in RenderViewSnapshot view,
        out Matrix4x4 lightView,
        out Matrix4x4 lightProj,
        float shadowDistance,
        int shadowMapSize,
        float zPad = 1f)
    {
        var dir = Vector3.Normalize(lightDirection);
        var worldUp = MathF.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;

        var near = view.ProjectionInfo.Near;
        var far = MathF.Min(view.ProjectionInfo.Far, near + shadowDistance);

        // camera frustum corners
        Span<Vector3> corners = stackalloc Vector3[8];
        GetFrustumCorners(in view, near, far, corners);

        var center = GetFrustumCenter(corners);
        var eye = center - dir * (shadowDistance * 0.5f);

        lightView = Matrix4x4.CreateLookAt(eye, center, worldUp);

        // light-space aabb frustum.
        Vector3 minLs = new(float.PositiveInfinity), maxLs = new(float.NegativeInfinity);
        for (var i = 0; i < 8; i++)
        {
            var p = Vector3.Transform(corners[i], lightView);
            minLs = Vector3.Min(minLs, p);
            maxLs = Vector3.Max(maxLs, p);
        }

        var snappedCenterLs = new Vector3(
            0.5f * (minLs.X + maxLs.X),
            0.5f * (minLs.Y + maxLs.Y),
            0.5f * (minLs.Z + maxLs.Z));

        Matrix4x4.Invert(lightView, out var invLightView);
        var snappedCenterWs = Vector3.Transform(snappedCenterLs, invLightView);
        var eyeSnapped = snappedCenterWs - dir * (shadowDistance * 0.5f);
        lightView = Matrix4x4.CreateLookAt(eyeSnapped, snappedCenterWs, worldUp);

        minLs = new Vector3(float.PositiveInfinity);
        maxLs = new Vector3(float.NegativeInfinity);
        for (int i = 0; i < 8; i++)
        {
            var p = Vector3.Transform(corners[i], lightView);
            minLs = Vector3.Min(minLs, p);
            maxLs = Vector3.Max(maxLs, p);
        }
        
        // texel snapping
        var size = maxLs - minLs;
        float ext = MathF.Max(size.X, size.Y);
        size.X = size.Y = ext;
        var texel = new Vector2(size.X / shadowMapSize, size.Y / shadowMapSize);
        minLs.X = MathF.Floor(minLs.X / texel.X) * texel.X;
        minLs.Y = MathF.Floor(minLs.Y / texel.Y) * texel.Y;
        maxLs.X = minLs.X + MathF.Ceiling(size.X / texel.X) * texel.X;
        maxLs.Y = minLs.Y + MathF.Ceiling(size.Y / texel.Y) * texel.Y;


        var nearLs = MathF.Max(0f, -maxLs.Z);
        var farLs = MathF.Max(nearLs + 0.001f, -minLs.Z) + zPad;

        lightProj = Matrix4x4.CreateOrthographicOffCenter(
            minLs.X, maxLs.X,
            minLs.Y, maxLs.Y,
            nearLs, farLs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 GetFrustumCenter(Span<Vector3> corners)
    {
        var s = Vector3.Zero;
        foreach (var c in corners) s += c;
        return s / corners.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GetFrustumCorners(in RenderViewSnapshot view, float near, float far, Span<Vector3> corners)
    {
        var tanY = 1f / view.ProjectionMatrix.M22;
        var tanX = 1f / view.ProjectionMatrix.M11;

        var up = view.Up;
        var right = view.Right;
        var forward = view.Forward;

        // extents at near/far
        float nx = near * tanX, ny = near * tanY;
        float fx = far * tanX, fy = far * tanY;

        var nc = view.Position + forward * near;
        var fc = view.Position + forward * far;

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