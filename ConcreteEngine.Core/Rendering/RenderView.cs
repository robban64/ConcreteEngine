using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Scene;

namespace ConcreteEngine.Core.Rendering;

public sealed class RenderView
{
    private Matrix4x4 _viewMatrix;
    private Matrix4x4 _projectionMatrix;
    private Matrix4x4 _projectionViewMatrix;

    public ref readonly Matrix4x4 ViewMatrix => ref _viewMatrix;
    public ref readonly Matrix4x4 ProjectionMatrix => ref _projectionMatrix;
    public ref readonly Matrix4x4 ProjectionViewMatrix => ref _projectionViewMatrix;

    public Vector3 Position { get; private set; }
    public Vector3 Forward { get; private set; }
    public Vector3 Right { get; private set; }
    public Vector3 Up { get; private set; }

    public ProjectionInfo ProjectionInfo { get; private set; }

    private readonly Snapshot _snapshot = new();

    public RenderView()
    {
    }

    public RenderView(
        in Matrix4x4 viewMatrix,
        in Matrix4x4 projectionMatrix,
        in Matrix4x4 projViewMat,
        Vector3 position)
    {
        _viewMatrix = viewMatrix;
        _projectionMatrix = projectionMatrix;
        _projectionViewMatrix = projViewMat;
        Position = position;
    }

    internal void PrepareFrame(Camera3D camera)
    {
        _viewMatrix = camera.ViewMatrix;
        _projectionMatrix = camera.ProjectionMatrix;
        _projectionViewMatrix = camera.ProjectionViewMatrix;
        Position = camera.Translation;
        Up = camera.Up;
        Right = camera.Right;
        Forward = camera.Forward;
        ProjectionInfo = new ProjectionInfo(camera.AspectRatio, camera.Fov, camera.NearPlane, camera.FarPlane);
    }

    internal void Restore()
    {
        _viewMatrix = _snapshot.ViewMatrix;
        _projectionMatrix = _snapshot.ProjectionMatrix;
        _projectionViewMatrix = _snapshot.ProjectionViewMatrix;
    }

    internal void ApplyLightView(Vector3 direction)
    {
        _snapshot.Commit(this);

        CreateDirLightView(direction, this,  out _viewMatrix, out _projectionMatrix, shadowMapSize: 2048);
        _projectionViewMatrix = _viewMatrix * _projectionMatrix;
    }

    private sealed class Snapshot
    {
        private Matrix4x4 _viewMatrix;
        private Matrix4x4 _projectionMatrix;
        private Matrix4x4 _projectionViewMatrix;

        public ref readonly Matrix4x4 ViewMatrix => ref _viewMatrix;
        public ref readonly Matrix4x4 ProjectionMatrix => ref _projectionMatrix;
        public ref readonly Matrix4x4 ProjectionViewMatrix => ref _projectionViewMatrix;

        public void Commit(RenderView view)
        {
            _viewMatrix = view.ViewMatrix;
            _projectionMatrix = view.ProjectionMatrix;
            _projectionViewMatrix = view.ProjectionViewMatrix;
        }
    }

    

    public static void CreateDirLightView(
        Vector3 lightDirection,
        RenderView camera,
        out Matrix4x4 lightView,
        out Matrix4x4 lightProj,
        float zPad = 1f,
        float shadowDistance = 60f,
        int shadowMapSize = 0)
    {
        var dir = Vector3.Normalize(lightDirection);
        var worldUp = MathF.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;

        float near = camera.ProjectionInfo.Near;
        float far  = MathF.Min(camera.ProjectionInfo.Far, near + shadowDistance);

        // Camera frustum corners
        Span<Vector3> corners = stackalloc Vector3[8];
        GetFrustumCorners(camera, near, far, corners);

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
        float farLS  = MathF.Max(nearLS + 0.001f, -minLS.Z) + zPad;

        lightProj = Matrix4x4.CreateOrthographicOffCenter(minLS.X, maxLS.X, minLS.Y, maxLS.Y, nearLS, farLS);
    }

    private static Vector3 GetFrustumCenter(Span<Vector3> corners)
    {
        var s = Vector3.Zero;
        for (var i = 0; i < corners.Length; i++) s += corners[i];
        return s / corners.Length;
    }

    private static void GetFrustumCorners(RenderView c, float near, float far, Span<Vector3> corners)
    {
        //float fovY = MathHelper.ToRadians(c.ProjectionInfo.Fov / 2f);
        //float tanY2 = MathF.Tan(0.5f * fovY);

        float tanY = 1f / c.ProjectionMatrix.M22;
        float tanX = 1f / c.ProjectionMatrix.M11;

        // extents at near/far
        float nx = near * tanX, ny = near * tanY;
        float fx = far * tanX, fy = far * tanY;

        var nc = c.Position + c.Forward * near;
        var fc = c.Position + c.Forward * far;

        // NearPlane plane
        corners[0] = nc + c.Up * ny - c.Right * nx; // NT-L
        corners[1] = nc + c.Up * ny + c.Right * nx; // NT-R
        corners[2] = nc - c.Up * ny - c.Right * nx; // NB-L
        corners[3] = nc - c.Up * ny + c.Right * nx; // NB-R

        // FarPlane plane
        corners[4] = fc + c.Up * fy - c.Right * fx; // FT-L
        corners[5] = fc + c.Up * fy + c.Right * fx; // FT-R
        corners[6] = fc - c.Up * fy - c.Right * fx; // FB-L
        corners[7] = fc - c.Up * fy + c.Right * fx; // FB-R
    }
}