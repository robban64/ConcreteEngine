using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Configuration;

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

        Frustum.Frustum.UpdateFrom(viewMatrix * projMatrix);
    }

    [SkipLocalsInit]
    private void UpdateLightView(int shadowSize, float shadowDist, float shadowZPad, Vector3 lightDirection)
    {
        Span<Vector3> corners = stackalloc Vector3[8];

        var projection = Camera.ProjectionInfo;
        var nearFar = new Vector2(projection.Near, MathF.Min(projection.Far, projection.Near + shadowDist));
        FrustumMath.FillFrustumCorners(in Camera.ViewMatrix, Camera.Translation, Camera.Tan, nearFar, corners);
        CameraUtils.CreateLightView(LightTransforms, shadowSize, shadowDist, shadowZPad, lightDirection, corners);
    }
    
}

file static class CameraUtils
{
    public static void CreateLightView(
        CameraTransformSnapshot transforms,
        int shadowSize,
        float shadowDistance,
        float shadowZPad,
        Vector3 lightDirection,
        Span<Vector3> corners
    )
    {
        var dir = Vector3.Normalize(lightDirection);
        var worldUp = MathF.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;

        var center = FrustumMath.GetFrustumCenter(corners);
        var farthestDistSqr = 0f;
        foreach (ref readonly var c in corners)
        {
            var d = Vector3.DistanceSquared(center, c);
            if (d > farthestDistSqr) farthestDistSqr = d;
        }

        var radius = MathF.Sqrt(farthestDistSqr);
        var diameter = radius * 2.0f;

        var shadowRotation = Matrix4x4.CreateLookAt(default, -dir, worldUp);
        var centerLs = Vector3.Transform(center, shadowRotation);

        var texelSize = diameter / shadowSize;
        var snappedX = MathF.Floor(centerLs.X / texelSize) * texelSize;
        var snappedY = MathF.Floor(centerLs.Y / texelSize) * texelSize;

        var snappedCenterLs = new Vector3(snappedX, snappedY, centerLs.Z);

        Matrix4x4.Invert(shadowRotation, out var invShadowRotation);
        var snappedCenterWorld = Vector3.Transform(snappedCenterLs, invShadowRotation);

        var eye = snappedCenterWorld - dir * shadowDistance * 0.5f;
        transforms.ViewMatrix = Matrix4x4.CreateLookAt(eye, snappedCenterWorld, worldUp);

        var minZ = float.MaxValue;
        var maxZ = float.MinValue;

        foreach (ref readonly var c in corners)
        {
            var z = Vector3.Transform(c, transforms.ViewMatrix).Z;
            if (z < minZ) minZ = z;
            if (z > maxZ) maxZ = z;
        }

        var nearLs = -maxZ - shadowZPad;
        var farLs = -minZ + shadowZPad;

        transforms.ProjectionMatrix = Matrix4x4.CreateOrthographic(diameter, diameter, nearLs, farLs);
    }
}