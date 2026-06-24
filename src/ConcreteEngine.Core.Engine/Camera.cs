using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Visuals;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine;


public sealed class Camera
{
    private const float MinNearPlane = 0.1f;
    private const float MaxNearPlane = 4f;

    private const float MinFarPlane = 5f;
    private const float MaxFarPlane = 10_000f;

    private const float MinFov = 10;
    private const float MaxFov = 179;

    private const float DirtyThreshold = MetricUnits.Micrometer;

    public ulong Version { get; private set; }

    private bool _dirty;

    private float _viewZ;

    private ProjectionInfo _projection = new(70, 0.1f, 500);
    private ViewTransform _transform, _prevTransform;

    internal readonly CameraTransform Transform;

    public Vector3 Forward { get; private set; }
    public Vector3 Up { get; private set; }
    public Vector3 Right { get; private set; }


    public Camera(Size2D viewport)
    {
        if(viewport < 128) Throwers.InvalidArgument(nameof(viewport));
        Transform = new CameraTransform();
        AspectRatio = viewport.AspectRatio;
        Ensure();
        _dirty = true;
    }


    internal Vector2 Tan => new(1f / Transform.ProjectionMatrix.M11, 1f / Transform.ProjectionMatrix.M22);

    public ref readonly Matrix4x4 ViewMatrix => ref Transform.ViewMatrix;
    public ref readonly Matrix4x4 ProjectionMatrix => ref Transform.ProjectionMatrix;
    public ref readonly Matrix4x4 InverseProjectionViewMatrix => ref Transform.InverseProjectionViewMatrix;

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

        ref var viewMatrix = ref Transform.ViewMatrix;
        ref var projectionMatrix = ref Transform.ProjectionMatrix;

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
        Transform.InverseProjectionViewMatrix = invProjection * modelMatrix;

        _viewZ = ViewMatrix.M43;
        Up = Transform.Up;
        Right = Transform.Right;
        Forward = Transform.Forward;

        return isDirty;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ushort MakeDepthKey(Vector3 worldPos)
    {
        var d = Vector3.Dot(Forward, worldPos) - _viewZ;

        if (d <= _projection.Near) return 0;
        if (d >= _projection.Far) return ushort.MaxValue;

        var t = (d - _projection.Near) / (_projection.Far - _projection.Near);
        return (ushort)(t * ushort.MaxValue + 0.5f);
    }
}