using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.ECS.Render;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Graphics.Visuals;

namespace ConcreteEngine.Core.Engine;

public sealed class CameraManager
{
    public static readonly CameraManager Instance = new();

    public readonly Camera Camera;

    internal readonly CameraTransformSnapshot FrameTransforms;
    internal readonly CameraTransformSnapshot LightTransforms;

    private CameraManager()
    {
        if (Instance != null)
            throw new InvalidOperationException($"{nameof(CameraManager)} is already initialized");

        Camera = new Camera(EngineSettings.Current.Display.WindowSize);
        FrameTransforms = new CameraTransformSnapshot();
        LightTransforms = new CameraTransformSnapshot();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BeginUpdate() => Camera.BeginUpdate();

    internal void CommitUpdate(LightingSettings lightning)
    {
        Camera.Ensure();
        var shadow = lightning.Shadow;
        var lightDir = lightning.Sun.DirectionNormalized;
        CreateLightView(shadow.ShadowMapSize, shadow.Distance, shadow.ZPad, lightDir);
    }


    internal void CommitFrame(float alpha)
    {
        Camera.Interpolate(alpha, out var translation, out var orientation);

        var frameTransforms = FrameTransforms;
        frameTransforms.UpdateViewMatrix(translation, orientation);
        frameTransforms.ProjectionMatrix = Camera.ProjectionMatrix;
        frameTransforms.ProjectionViewMatrix = frameTransforms.ViewMatrix * frameTransforms.ProjectionMatrix;
        RenderEcs.Core.RenderSystem.BuildFrustum(frameTransforms, LightTransforms);
    }

    [SkipLocalsInit]
    private void CreateLightView(int shadowSize, float shadowDist, float shadowZPad, Vector3 lightDir)
    {
        Span<Vector3> corners = stackalloc Vector3[8];

        Camera.FillFrustumCorners(corners, shadowDist);
        var center = GetFrustumCenter(corners);

        var farthestDistSqr = CalculateDistance(corners, center);
        var diameter = float.Sqrt(farthestDistSqr) * 2.0f;

        var worldUp = float.Abs(Vector3.Dot(lightDir, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;

        var shadowRotation = Matrix4x4.CreateLookAt(Vector3.Zero, -lightDir, worldUp);
        Matrix4x4.Invert(shadowRotation, out var invShadowRotation);

        var centerLs = Vector3.Transform(center, shadowRotation);
        var texelSize = diameter / shadowSize;
        var snappedX = float.Floor(centerLs.X / texelSize) * texelSize;
        var snappedY = float.Floor(centerLs.Y / texelSize) * texelSize;

        var snappedCenterLs = new Vector3(snappedX, snappedY, centerLs.Z);
        var snappedCenterWorld = Vector3.Transform(snappedCenterLs, invShadowRotation);

        var eye = snappedCenterWorld - lightDir * shadowDist * 0.5f;

        var viewMatrix = Matrix4x4.CreateLookAt(eye, snappedCenterWorld, worldUp);
        var projectionMatrix = CreateLightProjection(corners, diameter, shadowZPad, viewMatrix);

        LightTransforms.ViewMatrix = viewMatrix;
        LightTransforms.ProjectionMatrix = projectionMatrix;
        LightTransforms.ProjectionViewMatrix = viewMatrix * projectionMatrix;
    }

    private static Matrix4x4 CreateLightProjection(Span<Vector3> corners, float diameter, float shadowZPad, Matrix4x4 viewMatrix)
    {
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

        return Matrix4x4.CreateOrthographic(diameter, diameter, nearLs, farLs);
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
        var s = Vector3.Zero;
        foreach (ref readonly var c in corners) s += c;
        return s / corners.Length;
    }
}