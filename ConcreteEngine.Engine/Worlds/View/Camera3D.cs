using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Renderer.State;
using ConcreteEngine.Shared.World;

namespace ConcreteEngine.Engine.Worlds.View;

// TODO improve
public sealed class Camera3D
{
    private const float MinNearPlane = 0.1f;
    private const float MaxNearPlane = 4f;

    private const float MinFarPlane = 5f;
    private const float MaxFarPlane = 10_000f;

    private const float MinFov = 10;
    private const float MaxFov = 180;

    private const float DirtyThreshold = MetricUnits.Micrometer;

    private BoundingFrustum _frustum;
    private ViewMatrixData _renderView;

    private Matrix4x4 _viewMatrix = Matrix4x4.Identity;
    private Matrix4x4 _projectionMatrix = Matrix4x4.Identity;
    private Matrix4x4 _projectionViewMatrix = Matrix4x4.Identity;
    private Matrix4x4 _invProjectionViewMatrix = Matrix4x4.Identity;

    private ViewTransform _transform;
    private ViewTransform _prevTransform;

    private ProjectionInfo _projInfo = new(70, 0.1f, 500);

    private Size2D _viewportSize;
    private bool _dirty;


    public long Generation { get; private set; } = 0;


    public Camera3D()
    {
        Ensure();
        _dirty = true;
    }

    public Vector3 Right => new(_viewMatrix.M11, _viewMatrix.M21, _viewMatrix.M31);
    public Vector3 Up => new Vector3(_viewMatrix.M12, _viewMatrix.M22, _viewMatrix.M32);
    public Vector3 Forward => -new Vector3(_viewMatrix.M13, _viewMatrix.M23, _viewMatrix.M33);

    internal ref readonly BoundingFrustum Frustum => ref _frustum;
    internal ref readonly Matrix4x4 InverseProjectionViewMatrix => ref _invProjectionViewMatrix;

    public float AspectRatio => _projInfo.AspectRatio;
    internal CameraRenderView RenderView => new(ref _renderView.ViewMatrix, ref _projInfo, ref _frustum);

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

    public Size2D Viewport
    {
        get => _viewportSize;
        set
        {
            if (_viewportSize == value) return;
            _viewportSize = value;
            _projInfo.AspectRatio = value.AspectRatio;
            _dirty = true;
        }
    }

    public float Fov
    {
        get => _projInfo.Fov;
        set
        {
            if (FloatMath.NearlyEqual(value, _projInfo.Fov, MetricUnits.Decimeter)) return;
            _projInfo.Fov = float.Clamp(value, MinFov, MaxFov);
            _dirty = true;
        }
    }


    public float FarPlane
    {
        get => _projInfo.Far;
        set
        {
            if (FloatMath.NearlyEqual(value, _projInfo.Far, MetricUnits.Millimeter)) return;
            _projInfo.Far = float.Min(float.Max(value, MinFarPlane), MaxFarPlane);
            _dirty = true;
        }
    }

    public float NearPlane
    {
        get => _projInfo.Near;
        set
        {
            if (FloatMath.NearlyEqual(value, _projInfo.Near, MetricUnits.Millimeter)) return;
            _projInfo.Near = float.Min(float.Max(value, MinNearPlane), MaxNearPlane);
            _dirty = true;
        }
    }


    internal void StartTick()
    {
        _prevTransform = _transform;
    }

    // before frame start
    internal void EndTick(RenderParamsSnapshot renderParams, RenderCamera renderCamera)
    {
        Ensure();

        ref readonly var shadows = ref renderParams.Shadows;
        var lightDir = renderParams.SunLight.Direction;
        var nearFar = new Vector2(_projInfo.Near, MathF.Min(_projInfo.Far, _projInfo.Near + shadows.Distance));
        Span<Vector3> corners = stackalloc Vector3[8];
        FrustumMath.FillFrustumCorners(in _viewMatrix, in _projectionMatrix,
            _transform.Translation, nearFar, corners);
        RenderTransform.CreateLightView(ref renderCamera.LightSpace, in shadows, lightDir, corners);
    }


    internal void WriteSnapshot(float alpha, RenderCamera renderCamera)
    {
        var camPos = Vector3.Lerp(_prevTransform.Translation, _transform.Translation, alpha);
        var camOri = YawPitch.LerpFixed(_prevTransform.Orientation, _transform.Orientation, alpha);

        MatrixMath.CreateFixedSizeModelMatrix(in camPos, RotationMath.YawPitchToQuaternion(camOri), out var viewMatrix);
        Matrix4x4.Invert(viewMatrix, out _renderView.ViewMatrix);

        _renderView.ProjectionMatrix = _projectionMatrix;
        _renderView.ProjectionViewMatrix = _renderView.ViewMatrix * _projectionMatrix;
        _frustum = new BoundingFrustum(in _renderView.ProjectionViewMatrix);

        renderCamera.RenderView = _renderView;
        renderCamera.Transform = _transform;
    }

    internal void Ensure()
    {
        if (!_dirty) return;
        _dirty = false;

        var fov = FloatMath.ToRadians(_projInfo.Fov / 2f);
        _projectionMatrix =
            Matrix4x4.CreatePerspectiveFieldOfView(fov, _projInfo.AspectRatio, _projInfo.Near, _projInfo.Far);

        var rotation = RotationMath.YawPitchToQuaternion(_transform.Orientation);
        MatrixMath.CreateFixedSizeModelMatrix(in _transform.Translation, in rotation, out var view);
        Matrix4x4.Invert(view, out _viewMatrix);

        Matrix4x4.Invert(_viewMatrix, out var invView);
        Matrix4x4.Invert(_projectionMatrix, out var invProjection);
        _invProjectionViewMatrix = invProjection * invView;

        _projectionViewMatrix = _viewMatrix * _projectionMatrix;

        Generation++;
    }

    internal void FillData(out EditorCameraState state)
    {
        state.Transform = _transform;
        state.Projection = _projInfo;
        state.Viewport = _viewportSize;
    }

    internal void SetFromData(in EditorCameraState state)
    {
        _transform = state.Transform;
        _prevTransform = state.Transform;
        _projInfo = state.Projection;
        _dirty = true;
    }
}