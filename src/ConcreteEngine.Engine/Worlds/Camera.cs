using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Engine.Worlds;

public sealed class Camera : EngineCamera
{
    private const float MinNearPlane = 0.1f;
    private const float MaxNearPlane = 4f;

    private const float MinFarPlane = 5f;
    private const float MaxFarPlane = 10_000f;

    private const float MinFov = 10;
    private const float MaxFov = 180;

    private const float DirtyThreshold = MetricUnits.Micrometer;

    private ViewTransform _transform;
    private ViewTransform _prevTransform;

    private ProjectionInfo _projInfo = new(70, 0.1f, 500);

    private Size2D _viewport;
    private bool _dirty;

    public long Generation { get; private set; }


    public Camera(Size2D viewport)
    {
        _viewport = viewport;
        Ensure();
        _dirty = true;
    }

    internal CameraRenderView RenderView => new(ref _renderViewMatrix, ref _projInfo, ref _frustum);

    public override Vector3 Translation
    {
        get => _transform.Translation;
        set
        {
            if (VectorMath.DistanceNearlyEqual(in value, in _transform.Translation, DirtyThreshold)) return;
            _transform.Translation = value;
            _dirty = true;
        }
    }

    public override YawPitch Orientation
    {
        get => _transform.Orientation;
        set
        {
            if (YawPitch.NearlyEqual(value, _transform.Orientation)) return;
            _transform.Orientation = value;
            _dirty = true;
        }
    }

    public override Size2D Viewport
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _viewport;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected set
        {
            if (_viewport == value) return;
            _viewport = value;
            _dirty = true;
        }
    }

    public override float Fov
    {
        get => _projInfo.Fov;
        set
        {
            if (FloatMath.NearlyEqual(value, _projInfo.Fov, MetricUnits.Decimeter)) return;
            _projInfo.Fov = float.Clamp(value, MinFov, MaxFov);
            _dirty = true;
        }
    }


    public override float FarPlane
    {
        get => _projInfo.Far;
        set
        {
            if (FloatMath.NearlyEqual(value, _projInfo.Far, MetricUnits.Millimeter)) return;
            _projInfo.Far = float.Min(float.Max(value, MinFarPlane), MaxFarPlane);
            _dirty = true;
        }
    }

    public override float NearPlane
    {
        get => _projInfo.Near;
        set
        {
            if (FloatMath.NearlyEqual(value, _projInfo.Near, MetricUnits.Millimeter)) return;
            _projInfo.Near = float.Min(float.Max(value, MinNearPlane), MaxNearPlane);
            _dirty = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void StartTick(Size2D viewport)
    {
        Viewport = viewport;
        _prevTransform = _transform;
    }

    // before frame start
    internal void EndTick(WorldVisual renderParams, ref ViewMatrixData lightView)
    {
        Ensure();

        ref readonly var shadows = ref renderParams.GetShadow();
        var nearFar = new Vector2(_projInfo.Near, MathF.Min(_projInfo.Far, _projInfo.Near + shadows.Distance));

        Span<Vector3> corners = stackalloc Vector3[8];
        FrustumMath.FillFrustumCorners(in _viewMatrix, in _projectionMatrix,
            _transform.Translation, nearFar, corners);

        var lightDir = renderParams.GetDirectionalLight().Direction;
        CameraUtils.CreateLightView(ref lightView, in shadows, lightDir, corners);
    }


    internal void WriteSnapshot(float alpha, RenderCamera renderCamera)
    {
        var camPos = Vector3.Lerp(_prevTransform.Translation, _transform.Translation, alpha);
        var camOri = YawPitch.LerpFixed(_prevTransform.Orientation, _transform.Orientation, alpha);

        ref var localRenderView = ref _renderViewMatrix;
        MatrixMath.CreateFixedSizeModelMatrix(in camPos, RotationMath.YawPitchToQuaternion(camOri),
            out localRenderView);
        Matrix4x4.Invert(localRenderView, out localRenderView);

        scoped ref var view = ref renderCamera.RenderView;
        view.ViewMatrix = localRenderView;
        view.ProjectionMatrix = _projectionMatrix;
        view.ProjectionViewMatrix = view.ViewMatrix * _projectionMatrix;
        _frustum = new BoundingFrustum(in view.ProjectionViewMatrix);

        renderCamera.Transform = _transform;
    }

    private void Ensure()
    {
        if (!_dirty) return;
        _dirty = false;

        var fov = FloatMath.ToRadians(_projInfo.Fov / 2f);
        _projectionMatrix =
            Matrix4x4.CreatePerspectiveFieldOfView(fov, _viewport.AspectRatio, _projInfo.Near, _projInfo.Far);

        var rotation = RotationMath.YawPitchToQuaternion(_transform.Orientation);
        MatrixMath.CreateFixedSizeModelMatrix(in _transform.Translation, in rotation, out var view);
        Matrix4x4.Invert(view, out _viewMatrix);

        Matrix4x4.Invert(_viewMatrix, out var invView);
        Matrix4x4.Invert(_projectionMatrix, out var invProjection);
        _invProjectionViewMatrix = invProjection * invView;

        _projectionViewMatrix = _viewMatrix * _projectionMatrix;

        Generation++;
    }
}