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
    public bool IsDirty { get; private set; }
    public float AspectRatio { get; private set; }

    internal readonly CameraTransform Transform;

    private ViewTransform _viewTransform, _prevViewTransform;

    public Camera(Size2D viewport)
    {
        if (viewport < 128) Throwers.InvalidArgument(nameof(viewport));
        Transform = new CameraTransform();
        AspectRatio = viewport.AspectRatio;
        Ensure();
        IsDirty = true;
    }


    [InputNumber(Segment = "Transform")]
    public Vector3 Translation
    {
        get => _viewTransform.Translation;
        set
        {
            if (VectorMath.DistanceNearlyEqual(in value, in _viewTransform.Translation, DirtyThreshold)) return;
            _viewTransform.Translation = value;
            IsDirty = true;
        }
    }

    [InputNumber(Segment = "Transform", Converter = typeof(Vector2))]
    public YawPitch Orientation
    {
        get => _viewTransform.Orientation;
        set
        {
            if (YawPitch.NearlyEqual(value, _viewTransform.Orientation)) return;
            _viewTransform.Orientation = value;
            IsDirty = true;
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
            IsDirty = true;
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
            IsDirty = true;
        }
    } = 70;


    //
    public Vector3 Forward => Transform.Forward;
    public Vector3 Up => Transform.Up;
    public Vector3 Right => Transform.Right;

    public ref readonly Matrix4x4 ViewMatrix => ref Transform.ViewMatrix;
    public ref readonly Matrix4x4 ProjectionMatrix => ref Transform.ProjectionMatrix;
    public ref readonly Matrix4x4 InverseProjectionViewMatrix => ref Transform.InverseProjectionViewMatrix;
    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BeginUpdate() => _prevViewTransform = _viewTransform;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Interpolate(float alpha, out ViewTransform transform)
    {
        transform = ViewTransform.Lerp(in _prevViewTransform, in _viewTransform, alpha);
    }

    internal bool Ensure()
    {
        var isDirty = IsDirty;
        if (!isDirty) return false;
        IsDirty = false;
        Version++;

        AspectRatio = EngineWindow.AspectRatio;

        MatrixMath.CreateFixedSizeModelMatrix(
            in _viewTransform.Translation,
            RotationMath.YawPitchToQuaternion(_viewTransform.Orientation),
            out var modelMatrix);

        ref var viewMatrix = ref Transform.ViewMatrix;
        Matrix4x4.Invert(modelMatrix, out viewMatrix);

        ref var projectionMatrix = ref Transform.ProjectionMatrix;
        projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            FloatMath.ToRadians(Fov * 0.5f),
            AspectRatio,
            NearFarPlane.X,
            NearFarPlane.Y
        );

        Matrix4x4.Invert(projectionMatrix, out var invProjection);
        Transform.InverseProjectionViewMatrix = invProjection * modelMatrix;

        return isDirty;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ushort MakeDepthKey(Vector3 worldPos)
    {
        const float maxValueF = 65535f;

        var d = Vector3.Dot(Forward, worldPos) - ViewMatrix.M43;
        var nearFar = NearFarPlane;
        if (d <= nearFar.X) return 0;
        if (d >= nearFar.Y) return ushort.MaxValue;

        var t = (d - nearFar.X) / (nearFar.Y - nearFar.X);
        return (ushort)(t * maxValueF + 0.5f);
    }
}