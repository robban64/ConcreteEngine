using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Graphics.Visuals;

namespace ConcreteEngine.Core.Engine;

public sealed class CameraManager
{
    public static readonly CameraManager Instance = new();

    public readonly Camera Camera;

    internal readonly CameraFrustum Frustum;
    internal readonly CameraTransformSnapshot FrameTransforms;
    internal readonly CameraTransformSnapshot LightTransforms;


    private CameraManager()
    {
        if (Instance != null)
            throw new InvalidOperationException($"{nameof(CameraManager)} is already initialized");

        Camera = new Camera(EngineSettings.Current.Display.WindowSize);
        Frustum = new CameraFrustum();
        FrameTransforms = new CameraTransformSnapshot();
        LightTransforms = new CameraTransformSnapshot();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BeginUpdate() => Camera.BeginUpdate();

    internal void CommitUpdate()
    {
        var shadow = VisualManager.Instance.Shadow;
        if (!Camera.Ensure() && !shadow.WasDirty) return;

        var shadowProj = shadow.Projection;
        var lightDir = VisualManager.Instance.Illumination.Direction;
        UpdateLightView(shadow.ShadowMapSize, shadowProj.Distance, shadowProj.ZPad, lightDir);
        Frustum.UpdateLight(in LightTransforms.ProjectionViewMatrix);
    }


    internal void CommitFrame(float alpha)
    {
        Camera.Interpolate(alpha, out var translation, out var orientation);

        var frameTransforms = FrameTransforms;
        frameTransforms.UpdateViewMatrix(translation, orientation);
        frameTransforms.ProjectionMatrix = Camera.ProjectionMatrix;
        frameTransforms.ProjectionViewMatrix = frameTransforms.ViewMatrix * frameTransforms.ProjectionMatrix;

        Frustum.UpdateMain(in frameTransforms.ProjectionViewMatrix);
    }

    [SkipLocalsInit]
    private void UpdateLightView(int shadowSize, float shadowDist, float shadowZPad, Vector3 lightDirection)
    {
        Span<Vector3> corners = stackalloc Vector3[8];
        FillFrustumCorners(corners, Camera, shadowDist);
        var center = GetFrustumCenter(corners);

        var farthestDistSqr = CalculateDistance(corners, center);

        var diameter = float.Sqrt(farthestDistSqr) * 2.0f;

        var dir = Vector3.Normalize(lightDirection);
        var worldUp = float.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;

        var shadowRotation = Matrix4x4.CreateLookAt(default, -dir, worldUp);
        Matrix4x4.Invert(shadowRotation, out var invShadowRotation);

        var centerLs = Vector3.Transform(center, shadowRotation);
        var texelSize = diameter / shadowSize;
        var snappedX = float.Floor(centerLs.X / texelSize) * texelSize;
        var snappedY = float.Floor(centerLs.Y / texelSize) * texelSize;

        var snappedCenterLs = new Vector3(snappedX, snappedY, centerLs.Z);
        var snappedCenterWorld = Vector3.Transform(snappedCenterLs, invShadowRotation);

        var eye = snappedCenterWorld - dir * shadowDist * 0.5f;

        ref var viewMatrix = ref LightTransforms.ViewMatrix;
        viewMatrix = Matrix4x4.CreateLookAt(eye, snappedCenterWorld, worldUp);

        var minZ = float.MaxValue;
        var maxZ = float.MinValue;
        foreach (ref readonly var c in corners)
        {
            var z = Vector3.Transform(c, viewMatrix).Z;
            minZ = float.Min(minZ, z);
            maxZ = float.Max(maxZ, z);
        }

        var nearLs = -maxZ - shadowZPad;
        var farLs = -minZ + shadowZPad;

        LightTransforms.ProjectionMatrix = Matrix4x4.CreateOrthographic(diameter, diameter, nearLs, farLs);
        LightTransforms.ProjectionViewMatrix = viewMatrix * LightTransforms.ProjectionMatrix;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float CalculateDistance(Span<Vector3> corners, Vector3 center)
    {
        var farthestDistSqr = 0f;
        foreach (ref readonly var c in corners)
        {
            var d = Vector3.DistanceSquared(center, c);
            farthestDistSqr = float.Max(farthestDistSqr, d);
        }

        return farthestDistSqr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 GetFrustumCenter(Span<Vector3> corners)
    {
        Vector3 s = default;
        foreach (ref readonly var c in corners) s += c;
        return s / corners.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FillFrustumCorners(Span<Vector3> corners, Camera camera, float distance)
    {
        var tan = camera.Transform.Tan;

        var near = camera.NearFarPlane.X;
        var far = float.Min(camera.NearFarPlane.Y, near + distance);

        // extents at near/far
        float nx = near * tan.X, ny = near * tan.Y;
        float fx = far * tan.X, fy = far * tan.Y;

        Vector3 translation = camera.Translation, forward = camera.Forward, up = camera.Up, right = camera.Right;

        var nc = translation + forward * near;
        var fc = translation + forward * far;

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