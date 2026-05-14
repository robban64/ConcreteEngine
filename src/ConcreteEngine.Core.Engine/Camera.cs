using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Visuals;

namespace ConcreteEngine.Core.Engine;

public sealed class CameraFrustum
{
    public BoundingFrustum Frustum;
}

public sealed class CameraTransforms
{
    public Vector3 Translation;
    public CameraMatrices FrameMatrices = CameraMatrices.CreateIdentity();
    public CameraMatrices LightMatrices = CameraMatrices.CreateIdentity();
}

public sealed class Camera
{
    private const float MinNearPlane = 0.1f;
    private const float MaxNearPlane = 4f;

    private const float MinFarPlane = 5f;
    private const float MaxFarPlane = 10_000f;

    private const float MinFov = 10;
    private const float MaxFov = 179;

    private const float DirtyThreshold = MetricUnits.Micrometer;

    public readonly CameraFrustum CameraFrustum = new();

    public ulong Version { get; private set; }
    
    private bool _dirty;
    private Size2D _viewport;
    private ProjectionInfo _projection = new(70, 0.1f, 500);

    private ViewTransform _transform;
    private ViewTransform _prevTransform;

    private Matrix4x4 _viewMatrix = Matrix4x4.Identity;
    private Matrix4x4 _projectionMatrix = Matrix4x4.Identity;
    private Matrix4x4 _invProjectionViewMatrix = Matrix4x4.Identity;


    public Camera(Size2D viewport)
    {
        ArgOutOfRangeThrower.ThrowIfSizeTooSmall(viewport, 128);
        _viewport = viewport;
        Ensure();
        _dirty = true;
    }

    public Vector3 Right => new(_viewMatrix.M11, _viewMatrix.M21, _viewMatrix.M31);
    public Vector3 Up => new(_viewMatrix.M12, _viewMatrix.M22, _viewMatrix.M32);
    public Vector3 Forward => new(-_viewMatrix.M13, -_viewMatrix.M23, -_viewMatrix.M33);

    public ref readonly Matrix4x4 ViewMatrix => ref _viewMatrix;
    public ref readonly Matrix4x4 ProjectionMatrix => ref _projectionMatrix;
    public ref readonly Matrix4x4 InverseProjectionViewMatrix => ref _invProjectionViewMatrix;


    public Vector3 Translation
    {
        get => _transform.Translation;
        set
        {
            if (VectorMath.DistanceNearlyEqual(in value, in _transform.Translation, DirtyThreshold)) return;
            _transform.Translation = value;
            _dirty = true;
        }
    }

    public YawPitch Orientation
    {
        get => _transform.Orientation;
        set
        {
            if (YawPitch.NearlyEqual(value, _transform.Orientation)) return;
            _transform.Orientation = value;
            _dirty = true;
        }
    }

    public Size2D Viewport
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _viewport;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (_viewport == value) return;
            _viewport = value;
            _dirty = true;
        }
    }

    public float Fov
    {
        get => _projection.Fov;
        set
        {
            if (FloatMath.NearlyEqual(value, _projection.Fov, MetricUnits.Decimeter)) return;
            _projection.Fov = float.Clamp(value, MinFov, MaxFov);
            _dirty = true;
        }
    }


    public float FarPlane
    {
        get => _projection.Far;
        set
        {
            if (FloatMath.NearlyEqual(value, _projection.Far, MetricUnits.Millimeter)) return;
            _projection.Far = float.Min(float.Max(value, MinFarPlane), MaxFarPlane);
            _dirty = true;
        }
    }

    public float NearPlane
    {
        get => _projection.Near;
        set
        {
            if (FloatMath.NearlyEqual(value, _projection.Near, MetricUnits.Millimeter)) return;
            _projection.Near = float.Min(float.Max(value, MinNearPlane), MaxNearPlane);
            _dirty = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BeginUpdate(Size2D viewport)
    {
        Viewport = viewport;
        _prevTransform = _transform;
    }


    internal void UpdateFrameView(CameraTransforms cameraTransforms, float alpha)
    {
        //Ensure();
        var t = ViewTransform.Lerp(in _prevTransform, in _transform, alpha);
        cameraTransforms.Translation = t.Translation;

        ref var frameView = ref cameraTransforms.FrameMatrices;
        ref var viewMatrix = ref frameView.ViewMatrix;

        MatrixMath.CreateFixedSizeModelMatrix(
            in t.Translation,
            RotationMath.YawPitchToQuaternion(t.Orientation),
            out viewMatrix);

        Matrix4x4.Invert(viewMatrix, out viewMatrix);

        frameView.ProjectionMatrix = _projectionMatrix;
        CameraFrustum.Frustum.UpdateFrom(viewMatrix * _projectionMatrix);
    }

    [SkipLocalsInit]
    internal void UpdateLightView(CameraTransforms cameraTransforms, int shadowSize, float shadowDist, float shadowZPad,
        Vector3 lightDirection)
    {
        //Ensure();
        Span<Vector3> corners = stackalloc Vector3[8];

        var nearFar = new Vector2(_projection.Near, MathF.Min(_projection.Far, _projection.Near + shadowDist));
        var tan = new Vector2(1f / _projectionMatrix.M11, 1f / _projectionMatrix.M22);
        FrustumMath.FillFrustumCorners(in _viewMatrix, _transform.Translation, tan, nearFar, corners);
        CameraUtils.CreateLightView(ref cameraTransforms.LightMatrices, shadowSize, shadowDist,
            shadowZPad, lightDirection, corners);
    }

    internal void Ensure()
    {
        if (!_dirty) return;
        _dirty = false;

        ref var viewMatrix = ref _viewMatrix;
        MatrixMath.CreateFixedSizeModelMatrix(
            in _transform.Translation,
            RotationMath.YawPitchToQuaternion(_transform.Orientation),
            out  var modelMatrix);

        Matrix4x4.Invert(modelMatrix, out viewMatrix);

        _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            FloatMath.ToRadians(_projection.Fov * 0.5f),
            _viewport.AspectRatio,
            _projection.Near,
            _projection.Far
        );

        Matrix4x4.Invert(_projectionMatrix, out var invProjection);
        _invProjectionViewMatrix = invProjection * modelMatrix;

        Version++;
    }
}


file static class CameraUtils
{
    public static void CreateLightView(
        scoped ref CameraMatrices view,
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
        view.ViewMatrix = Matrix4x4.CreateLookAt(eye, snappedCenterWorld, worldUp);

        var minZ = float.MaxValue;
        var maxZ = float.MinValue;

        foreach (ref readonly var c in corners)
        {
            var z = Vector3.Transform(c, view.ViewMatrix).Z;
            if (z < minZ) minZ = z;
            if (z > maxZ) maxZ = z;
        }

        var nearLs = -maxZ - shadowZPad;
        var farLs = -minZ + shadowZPad;

        view.ProjectionMatrix = Matrix4x4.CreateOrthographic( diameter, diameter, nearLs, farLs);

    }
}