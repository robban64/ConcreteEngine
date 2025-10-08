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

        CreateDirLightView(direction, this, 0.1f, 60f, out _viewMatrix, out _projectionMatrix, shadowMapSize: 2048,
            padding: 1);
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
        float cameraNear,
        float cameraFar,
        out Matrix4x4 lightView,
        out Matrix4x4 lightProj,
        float padding = 0.5f,
        int shadowMapSize = 0)
    {
        var dir = Vector3.Normalize(lightDirection);

        var worldUp = MathF.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;

        // Camera frustum corners
        Span<Vector3> corners = stackalloc Vector3[8];
        GetFrustumCorners(camera, cameraNear, cameraFar, corners);

        var center = GetFrustumCenter(corners);

        //  Position light camera 
        var sliceDepth = cameraFar - cameraNear;
        var eye = center - dir * (sliceDepth + padding * 2f);

        lightView = Matrix4x4.CreateLookAt(eye, center, worldUp);

        //Light-space AABB of frustum.
        Vector3 minLS = new(float.PositiveInfinity), maxLS = new(float.NegativeInfinity);
        for (int i = 0; i < 8; i++)
        {
            var p = Vector3.Transform(corners[i], lightView);
            minLS = Vector3.Min(minLS, p);
            maxLS = Vector3.Max(maxLS, p);
        }

        // Guard band in XY; keep Z conservative to avoid clipping.
        minLS.X -= padding;
        minLS.Y -= padding;
        maxLS.X += padding;
        maxLS.Y += padding;

        // Optional texel snapping (independent of screen resolution).
        if (shadowMapSize > 0)
        {
            var size = maxLS - minLS;
            var texelX = size.X / shadowMapSize;
            var texelY = size.Y / shadowMapSize;

            minLS.X = MathF.Floor(minLS.X / texelX) * texelX;
            minLS.Y = MathF.Floor(minLS.Y / texelY) * texelY;
            maxLS.X = minLS.X + MathF.Ceiling(size.X / texelX) * texelX;
            maxLS.Y = minLS.Y + MathF.Ceiling(size.Y / texelY) * texelY;
        }

        // Right-handed orthographic
        float zNear = MathF.Max(0f, -maxLS.Z);
        float zFar = MathF.Max(zNear + 0.001f, -minLS.Z);

        lightProj = Matrix4x4.CreateOrthographicOffCenter(minLS.X, maxLS.X, minLS.Y, maxLS.Y, zNear, zFar);
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


    /*
    private static void CreateDirLightMatrices(Vector3 lightDir, RenderView camera,
        out Matrix4x4 viewMat, out Matrix4x4 projMat)
    {
        const float padding = 2;
        const int shadowMapSize = 2048;
        var near = 0.1f;
        var far = 60f;

        var dir = Vector3.Normalize(lightDir);

        var worldUp = Math.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;

        Span<Vector3> corners = stackalloc Vector3[8];
        GetFrustumCorners(camera, near, far, corners);
        var center = GetFrustumCenter(corners);

        var eye = center - dir * (far * 0.5f + padding);

        viewMat = Matrix4x4.CreateLookAt(eye, center, worldUp);

        Vector3 minLs = new(float.PositiveInfinity), maxLs = new(float.NegativeInfinity);
        for (var i = 0; i < 8; i++)
        {
            var ls = Vector3.Transform(corners[i], viewMat);
            minLs = Vector3.Min(minLs, ls);
            maxLs = Vector3.Max(maxLs, ls);
        }

        //  guard band
        minLs -= new Vector3(padding, padding, 0);
        maxLs += new Vector3(padding, padding, 0);

        // Texel snapping
        if (shadowMapSize > 0)
        {
            var sizeLs = maxLs - minLs;
            var texelSizeX = sizeLs.X / shadowMapSize;
            var texelSizeY = sizeLs.Y / shadowMapSize;

            minLs.X = MathF.Floor(minLs.X / texelSizeX) * texelSizeX;
            minLs.Y = MathF.Floor(minLs.Y / texelSizeY) * texelSizeY;
            maxLs.X = minLs.X + MathF.Ceiling(sizeLs.X / texelSizeX) * texelSizeX;
            maxLs.Y = minLs.Y + MathF.Ceiling(sizeLs.Y / texelSizeY) * texelSizeY;
        }

        var zMin = minLs.Z;
        var zMax = maxLs.Z;

        var nearDist = (zMax > 0f) ? 0f : -zMax; // clamp to 0 if corner
        var farDist = -zMin;

        nearDist = MathF.Max(nearDist, 0.0f);
        farDist = MathF.Max(farDist, nearDist + 0.001f);

        projMat = Matrix4x4.CreateOrthographicOffCenter(minLs.X, maxLs.X, minLs.Y, maxLs.Y, nearDist, farDist);
    }
 */
}