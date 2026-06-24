using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Graphics;

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

    internal void CommitUpdate(VisualManager visuals)
    {
        var shadow = visuals.Shadow;
        if (!Camera.Ensure() && !shadow.WasDirty) return;

        var shadowProj = shadow.Projection;
        var lightDir = visuals.Illumination.DirectionalLight.Value.Direction;

        UpdateLightView(shadow.ShadowMapSize, shadowProj.Value.Distance, shadowProj.Value.ZPad, lightDir);
        
    }



    internal void CommitFrame(float alpha)
    {
        Camera.Interpolate(alpha, out var viewTransform);
        FrameTransforms.Translation = viewTransform.Translation;

        ref var viewMatrix = ref FrameTransforms.ViewMatrix;
        ref var projMatrix = ref FrameTransforms.ProjectionMatrix;

        MatrixMath.CreateFixedSizeModelMatrix(
            in viewTransform.Translation,
            RotationMath.YawPitchToQuaternion(viewTransform.Orientation),
            out viewMatrix);

        Matrix4x4.Invert(viewMatrix, out viewMatrix);

        projMatrix = Camera.ProjectionMatrix;

        Frustum.Update(viewMatrix * projMatrix);
    }

    [SkipLocalsInit]
    private void UpdateLightView(int shadowSize, float shadowDist, float shadowZPad, Vector3 lightDirection)
    {
        Span<Vector3> corners = stackalloc Vector3[8];
        CameraUtils.FillFrustumCorners(corners, Camera, shadowDist);
        var center = CameraUtils.GetFrustumCenter(corners);
        
        var farthestDistSqr = 0f;
        foreach (ref readonly var c in corners)
        {
            var d = Vector3.DistanceSquared(center, c);
            if (d > farthestDistSqr) farthestDistSqr = d;
        }
        var diameter = MathF.Sqrt(farthestDistSqr) * 2.0f;

        var dir = Vector3.Normalize(lightDirection);
        var worldUp = MathF.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;

        var shadowRotation = Matrix4x4.CreateLookAt(default, -dir, worldUp);
        Matrix4x4.Invert(shadowRotation, out var invShadowRotation);

        var centerLs = Vector3.Transform(center, shadowRotation);
        var texelSize = diameter / shadowSize;
        var snappedX = MathF.Floor(centerLs.X / texelSize) * texelSize;
        var snappedY = MathF.Floor(centerLs.Y / texelSize) * texelSize;

        var snappedCenterLs = new Vector3(snappedX, snappedY, centerLs.Z);
        var snappedCenterWorld = Vector3.Transform(snappedCenterLs, invShadowRotation);

        var eye = snappedCenterWorld - dir * shadowDist * 0.5f;
        LightTransforms.ViewMatrix = Matrix4x4.CreateLookAt(eye, snappedCenterWorld, worldUp);

        var minZ = float.MaxValue;
        var maxZ = float.MinValue;

        foreach (ref readonly var c in corners)
        {
            var z = Vector3.Transform(c, LightTransforms.ViewMatrix).Z;
            if (z < minZ) minZ = z;
            if (z > maxZ) maxZ = z;
        }

        var nearLs = -maxZ - shadowZPad;
        var farLs = -minZ + shadowZPad;

        LightTransforms.ProjectionMatrix = Matrix4x4.CreateOrthographic(diameter, diameter, nearLs, farLs);
    }
    
}

file static class CameraUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetFrustumCenter(Span<Vector3> corners)
    {
        Vector3 s = default;
        foreach (ref readonly var c in corners) s += c;
        return s / corners.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillFrustumCorners(Span<Vector3> corners, Camera camera, float distance)
    {
        var tan = camera.Transform.Tan;

        var near = camera.NearPlane;
        var far = MathF.Min(camera.FarPlane, near + distance);
        Vector3 forward = camera.Forward, up = camera.Up, right = camera.Right;

        // extents at near/far
        float nx = near * tan.X, ny = near * tan.Y;
        float fx = far * tan.X, fy = far * tan.Y;

        var nc = camera.Translation + forward * near;
        var fc = camera.Translation + forward * far;

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