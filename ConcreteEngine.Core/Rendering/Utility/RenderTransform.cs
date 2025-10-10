#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Rendering.State;

#endregion

namespace ConcreteEngine.Core.Rendering.Utility;

internal static class RenderTransform
{
    public static void CreateDirLightView(
        Vector3 lightDirection,
        in RenderViewSnapshot view,
        out Matrix4x4 lightView,
        out Matrix4x4 lightProj,
        float zPad = 1f,
        float shadowDistance = 60f,
        int shadowMapSize = 0)
    {
        var dir = Vector3.Normalize(lightDirection);
        var worldUp = MathF.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;

        float near = view.ProjectionInfo.Near;
        float far = MathF.Min(view.ProjectionInfo.Far, near + shadowDistance);

        // Camera frustum corners
        Span<Vector3> corners = stackalloc Vector3[8];
        GetFrustumCorners(in view, near, far, corners);

        var center = GetFrustumCenter(corners);
        var eye = center - dir * (shadowDistance * 0.5f);

        lightView = Matrix4x4.CreateLookAt(eye, center, worldUp);

        //Light-space AABB of frustum.
        Vector3 minLS = new(float.PositiveInfinity), maxLS = new(float.NegativeInfinity);
        for (int i = 0; i < 8; i++)
        {
            var p = Vector3.Transform(corners[i], lightView);
            minLS = Vector3.Min(minLS, p);
            maxLS = Vector3.Max(maxLS, p);
        }

        // Texel snapping
        if (shadowMapSize > 0)
        {
            var size = maxLS - minLS;
            var texel = new Vector2(size.X / shadowMapSize, size.Y / shadowMapSize);
            minLS.X = MathF.Floor(minLS.X / texel.X) * texel.X;
            minLS.Y = MathF.Floor(minLS.Y / texel.Y) * texel.Y;
            maxLS.X = minLS.X + MathF.Ceiling(size.X / texel.X) * texel.X;
            maxLS.Y = minLS.Y + MathF.Ceiling(size.Y / texel.Y) * texel.Y;
        }

        // Near/Far with zPad
        float nearLS = MathF.Max(0f, -maxLS.Z);
        float farLS = MathF.Max(nearLS + 0.001f, -minLS.Z) + zPad;

        lightProj = Matrix4x4.CreateOrthographicOffCenter(minLS.X, maxLS.X, minLS.Y, maxLS.Y, nearLS, farLS);
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

        // extents at near/far
        float nx = near * tanX, ny = near * tanY;
        float fx = far * tanX, fy = far * tanY;

        var nc = view.Position + view.Forward * near;
        var fc = view.Position + view.Forward * far;

        // NearPlane plane
        corners[0] = nc + view.Up * ny - view.Right * nx; // NT-L
        corners[1] = nc + view.Up * ny + view.Right * nx; // NT-R
        corners[2] = nc - view.Up * ny - view.Right * nx; // NB-L
        corners[3] = nc - view.Up * ny + view.Right * nx; // NB-R

        // FarPlane plane
        corners[4] = fc + view.Up * fy - view.Right * fx; // FT-L
        corners[5] = fc + view.Up * fy + view.Right * fx; // FT-R
        corners[6] = fc - view.Up * fy - view.Right * fx; // FB-L
        corners[7] = fc - view.Up * fy + view.Right * fx; // FB-R
    }
}