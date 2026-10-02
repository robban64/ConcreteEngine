using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Graphics.Visuals;

// ReSharper disable ReplaceWithFieldKeyword

namespace ConcreteEngine.Core.Engine;

[Inspect]
public sealed class Camera
{
    private const float MinNearPlane = 0.1f;
    private const float MaxNearPlane = 4f;

    private const float MinFarPlane = 5f;
    private const float MaxFarPlane = 10_000f;

    private const float MinFov = 10f;
    private const float MaxFov = 179f;

    public static Camera Main { get; private set; } = null!;

    internal readonly CameraTransform Transform;
    internal readonly CameraTransformSnapshot FrameTransforms;
    internal readonly CameraTransformSnapshot LightTransforms;

    public bool IsDirty { get; private set; }
    public float AspectRatio { get; private set; }

    private float _fov = 70f;
    private Vector2 _nearFarPlane = new(0.1f, 500f);

    private Vector3 _translation, _lastTranslation;
    private Vector2 _orientation, _lastOrientation;

    public Camera(Size2D viewport)
    {
        if (Main != null!) Throwers.InvalidOperation(nameof(Main));
        if (viewport < 128) Throwers.InvalidArgument(nameof(viewport));

        Transform = new CameraTransform();
        FrameTransforms = new CameraTransformSnapshot();
        LightTransforms = new CameraTransformSnapshot();
        AspectRatio = viewport.AspectRatio;
        Ensure();
        IsDirty = true;

        Main = this;
    }

    //
    public Vector3 Forward => Transform.Forward;
    public Vector3 Up => Transform.Up;
    public Vector3 Right => Transform.Right;

    public ref readonly Matrix4x4 ViewMatrix => ref Transform.ViewMatrix;
    public ref readonly Matrix4x4 ProjectionMatrix => ref Transform.ProjectionMatrix;
    public ref readonly Matrix4x4 InverseProjectionViewMatrix => ref Transform.InverseProjectionViewMatrix;
    //


    internal void SetAspectRatio(float aspectRatio)
    {
        AspectRatio = aspectRatio;
        IsDirty = true;
    }


    [InputNumber]
    public Vector3 Translation
    {
        get => _translation;
        set
        {
            _translation = value;
            IsDirty = true;
        }
    }

    [InputNumber(Label = "Orientation")]
    public Vector2 Orientation
    {
        get => _orientation;
        set
        {
            _orientation = value;
            IsDirty = true;
        }
    }

    [InputNumber(Label = "Near & Far")]
    public Vector2 NearFarPlane
    {
        get => _nearFarPlane;
        set
        {
            if (VectorMath.NearlyEqual(value, _nearFarPlane, MetricUnits.Millimeter)) return;
            _nearFarPlane.X = float.Min(float.Max(value.X, MinNearPlane), MaxNearPlane);
            _nearFarPlane.Y = float.Min(float.Max(value.Y, MinFarPlane), MaxFarPlane);
            IsDirty = true;
        }
    }

    [InputNumber(InputStyle.Slider, Label = "Field of view", Min = 10f, Max = 179f)]
    public float Fov
    {
        get => _fov;
        set
        {
            if (FloatMath.NearlyEqual(value, _fov, MetricUnits.Millimeter)) return;
            _fov = float.Clamp(value, MinFov, MaxFov);
            IsDirty = true;
        }
    }

    internal void BeginUpdate()
    {
        _lastTranslation = _translation;
        _lastOrientation = _orientation;
    }
    
    internal void Commit(LightingSettings lightning)
    {
        Ensure();

        var shadow = lightning.Shadow;
        var lightDir = lightning.Sun.DirectionNormalized;
        CreateLightView(shadow.ShadowMapSize, shadow.Distance, shadow.ZPad, lightDir);
    }

    internal void UpdateFrame(float alpha)
    {
        var translation = Vector3.Lerp(_lastTranslation, _translation, alpha);
        var orientation = RotationMath.LerpYawPitch(_lastOrientation, _orientation, alpha);
        FrameTransforms.From(translation, orientation, in ProjectionMatrix);
    }

    private void Ensure()
    {
        if (!IsDirty) return;
        IsDirty = false;

        var fov = FloatMath.ToRadians(Fov * 0.5f);
        var quaternion = RotationMath.YawPitchToQuaternion(_orientation);
        MatrixMath.CreateFixedSizeModelMatrix(_translation, in quaternion, out var modelMatrix);

        ref var viewMatrix = ref Transform.ViewMatrix;
        Matrix4x4.Invert(modelMatrix, out viewMatrix);

        ref var projectionMatrix = ref Transform.ProjectionMatrix;
        projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(fov, AspectRatio, _nearFarPlane.X, _nearFarPlane.Y);

        Matrix4x4.Invert(projectionMatrix, out var invProjection);
        Transform.InverseProjectionViewMatrix = invProjection * modelMatrix;
    }


    [SkipLocalsInit]
    private void CreateLightView(int shadowSize, float shadowDist, float shadowZPad, Vector3 lightDir)
    {
        Span<Vector3> corners = stackalloc Vector3[8];

        FillFrustumCorners(corners, shadowDist);
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

    private void FillFrustumCorners(Span<Vector3> corners, float distance)
    {
        var tan = Transform.Tan;
        var nearFar = _nearFarPlane;
        nearFar.Y = float.Min(nearFar.Y, nearFar.X + distance);

        // extents at near/far
        float nx = nearFar.X * tan.X, ny = nearFar.X * tan.Y;
        float fx = nearFar.Y * tan.X, fy = nearFar.Y * tan.Y;

        Vector3 forward = Forward, up = Up, right = Right;

        var nc = Translation + forward * nearFar.X;
        var fc = Translation + forward * nearFar.Y;

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


    private static Matrix4x4 CreateLightProjection(Span<Vector3> corners, float diameter, float shadowZPad,
        Matrix4x4 viewMatrix)
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