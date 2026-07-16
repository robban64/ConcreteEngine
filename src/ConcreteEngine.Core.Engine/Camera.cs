using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Visuals;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine;

[Inspect]
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

    private ViewTransform _transform, _prevTransform;

    internal readonly CameraTransform Transform;

    public Vector3 Forward { get; private set; }
    public Vector3 Up { get; private set; }
    public Vector3 Right { get; private set; }

    public float AspectRatio { get; private set; }

    public Camera(Size2D viewport)
    {
        if (viewport < 128) Throwers.InvalidArgument(nameof(viewport));
        Transform = new CameraTransform();
        AspectRatio = viewport.AspectRatio;
        Ensure();
        _dirty = true;
    }


    [InputNumber(Segment = "Transform")]
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

    [InputNumber(Segment = "Transform", Converter = typeof(Vector2))]
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

    [InputNumber(Label = "Near & Far", Segment = "Projection")]
    public Vector2 NearFarPlane
    {
        get;
        set
        {
            if (VectorMath.NearlyEqual(value, field, MetricUnits.Millimeter)) return;
            field.X = float.Min(float.Max(value.X, MinNearPlane), MaxNearPlane);
            field.Y = float.Min(float.Max(value.Y, MinFarPlane), MaxFarPlane);
            _dirty = true;
        }
    } = new(0.1f, 500f);

    [InputNumber(InputStyle.Slider, Label = "Field of view", Min = 10f, Max = 179f, Segment = "Projection")]
    public float Fov
    {
        get;
        set
        {
            if (FloatMath.NearlyEqual(value, field, MetricUnits.Decimeter)) return;
            field = float.Clamp(value, MinFov, MaxFov);
            _dirty = true;
        }
    } = 70;


    //
    public ref readonly Matrix4x4 ViewMatrix => ref Transform.ViewMatrix;
    public ref readonly Matrix4x4 ProjectionMatrix => ref Transform.ProjectionMatrix;
    public ref readonly Matrix4x4 InverseProjectionViewMatrix => ref Transform.InverseProjectionViewMatrix;
    //

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

        AspectRatio = EngineWindow.Viewport.Size.AspectRatio;

        ref var viewMatrix = ref Transform.ViewMatrix;
        ref var projectionMatrix = ref Transform.ProjectionMatrix;

        MatrixMath.CreateFixedSizeModelMatrix(
            in _transform.Translation,
            RotationMath.YawPitchToQuaternion(_transform.Orientation),
            out var modelMatrix);

        Matrix4x4.Invert(modelMatrix, out viewMatrix);

        projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            FloatMath.ToRadians(Fov * 0.5f),
            AspectRatio,
            NearFarPlane.X,
            NearFarPlane.Y
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
        var nearFar = NearFarPlane;
        if (d <= nearFar.X) return 0;
        if (d >= nearFar.Y) return ushort.MaxValue;

        var t = (d - nearFar.X) / (nearFar.Y - nearFar.X);
        return (ushort)(t * ushort.MaxValue + 0.5f);
    }
}