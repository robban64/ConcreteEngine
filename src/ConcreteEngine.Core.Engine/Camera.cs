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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IntersectsBox(in BoundingBox box)
    {
        for (int i = 0; i < 6; i++)
        {
            if (CollisionMethods.IsOutsidePlane(in box, in Unsafe.Add(ref Frustum.LeftPlane, i))) return false;
        }

        return true;
    }
}

public sealed class CameraTransformSnapshot
{
    public Vector3 Translation;
    public Matrix4x4 ViewMatrix;
    public Matrix4x4 ProjectionMatrix;

    public Vector3 Right => new(ViewMatrix.M11, ViewMatrix.M21, ViewMatrix.M31);
    public Vector3 Up => new(ViewMatrix.M12, ViewMatrix.M22, ViewMatrix.M32);
    public Vector3 Forward => new(-ViewMatrix.M13, -ViewMatrix.M23, -ViewMatrix.M33);
}

public sealed class CameraTransforms
{
    public Matrix4x4 ViewMatrix;
    public Matrix4x4 ProjectionMatrix;
    public Matrix4x4 InverseProjectionViewMatrix;

    public Vector3 Right => new(ViewMatrix.M11, ViewMatrix.M21, ViewMatrix.M31);
    public Vector3 Up => new(ViewMatrix.M12, ViewMatrix.M22, ViewMatrix.M32);
    public Vector3 Forward => new(-ViewMatrix.M13, -ViewMatrix.M23, -ViewMatrix.M33);
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

    internal readonly CameraTransforms Transforms;

    public ulong Version { get; private set; }

    private bool _dirty;
    private ProjectionInfo _projection = new(70, 0.1f, 500);

    private ViewTransform _transform;
    private ViewTransform _prevTransform;

    public Vector3 Right { get; private set; }
    public Vector3 Up { get; private set; }
    public Vector3 Forward { get; private set; }

    public Camera(Size2D viewport)
    {
        ArgOutOfRangeThrower.ThrowIfSizeTooSmall(viewport, 128);
        Transforms = new CameraTransforms();
        AspectRatio = viewport.AspectRatio;
        Ensure();
        _dirty = true;
    }

    internal Vector2 Tan => new(1f / Transforms.ProjectionMatrix.M11, 1f / Transforms.ProjectionMatrix.M22);

    public ref readonly Matrix4x4 ViewMatrix => ref Transforms.ViewMatrix;
    public ref readonly Matrix4x4 ProjectionMatrix => ref Transforms.ProjectionMatrix;
    public ref readonly Matrix4x4 InverseProjectionViewMatrix => ref Transforms.InverseProjectionViewMatrix;

    public ref readonly ProjectionInfo ProjectionInfo => ref _projection;

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

    public float AspectRatio
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set
        {
            if (FloatMath.NearlyEqual(field, value)) return;
            field = value;
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
    internal void BeginUpdate() => _prevTransform = _transform;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Interpolate(float alpha, out ViewTransform transform)
    {
        transform = ViewTransform.Lerp(in _prevTransform, in _transform, alpha);
    }

    internal bool Ensure()
    {
        var isDirty = _dirty;
        if (!isDirty) return false;
        _dirty = false;
        Version++;

        ref var viewMatrix = ref Transforms.ViewMatrix;
        ref var projectionMatrix = ref Transforms.ProjectionMatrix;

        MatrixMath.CreateFixedSizeModelMatrix(
            in _transform.Translation,
            RotationMath.YawPitchToQuaternion(_transform.Orientation),
            out var modelMatrix);

        Matrix4x4.Invert(modelMatrix, out viewMatrix);

        projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            FloatMath.ToRadians(_projection.Fov * 0.5f),
            AspectRatio,
            _projection.Near,
            _projection.Far
        );

        Matrix4x4.Invert(projectionMatrix, out var invProjection);
        Transforms.InverseProjectionViewMatrix = invProjection * modelMatrix;

        Up = Transforms.Up;
        Right = Transforms.Right;
        Forward = Transforms.Forward;

        return isDirty;
    }
}