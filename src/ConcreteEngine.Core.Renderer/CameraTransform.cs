using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Core.Renderer.Visuals;

namespace ConcreteEngine.Core.Renderer;

public sealed class CameraTransform
{
    private const float MinNearPlane = 0.1f;
    private const float MaxNearPlane = 4f;

    private const float MinFarPlane = 5f;
    private const float MaxFarPlane = 10_000f;

    private const float MinFov = 10;
    private const float MaxFov = 179;

    private const float DirtyThreshold = MetricUnits.Micrometer;

    private Size2D _viewport;
    private ProjectionInfo _projInfo = new(70, 0.1f, 500);

    private ViewTransform _transform;
    private ViewTransform _prevTransform;

    private Matrix4x4 _viewMatrix = Matrix4x4.Identity;
    private Matrix4x4 _projectionMatrix = Matrix4x4.Identity;
    private Matrix4x4 _projectionViewMatrix = Matrix4x4.Identity;
    private Matrix4x4 _invProjectionViewMatrix = Matrix4x4.Identity;

    private CameraMatrices _frameMatrices = CameraMatrices.CreateIdentity();
    private CameraMatrices _lightMatrices = CameraMatrices.CreateIdentity();

    private BoundingFrustum _frustum;

    private bool _dirty;
    public long Generation { get; private set; }

    public CameraTransform(Size2D viewport)
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
    public ref readonly Matrix4x4 ProjectionViewMatrix => ref _projectionViewMatrix;
    public ref readonly Matrix4x4 InverseProjectionViewMatrix => ref _invProjectionViewMatrix;

    public ref readonly CameraMatrices GetFrameMatrices() => ref _frameMatrices;
    public ref readonly CameraMatrices GetLightMatrices() => ref _lightMatrices;

    public ref readonly BoundingFrustum GetFrustum() => ref _frustum;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginUpdate(Size2D viewport)
    {
        Viewport = viewport;
        _prevTransform = _transform;
    }


    public void UpdateFrameView(float alpha)
    {
        Ensure();

        var camPos = Vector3.Lerp(_prevTransform.Translation, _transform.Translation, alpha);
        var camOri = YawPitch.LerpFixed(_prevTransform.Orientation, _transform.Orientation, alpha);

        ref var frameView = ref _frameMatrices;
        ref var viewMatrix = ref frameView.ViewMatrix;
        MatrixMath.CreateFixedSizeModelMatrix(in camPos, RotationMath.YawPitchToQuaternion(camOri), out viewMatrix);
        Matrix4x4.Invert(viewMatrix, out frameView.ViewMatrix);

        frameView.ProjectionMatrix = _projectionMatrix;
        frameView.ProjectionViewMatrix = viewMatrix * _projectionMatrix;
        _frustum = new BoundingFrustum(in frameView.ProjectionViewMatrix);
    }

    public void EndUpdate(in ShadowParams shadow, Vector3 lightDirection)
    {
        Ensure();

        Span<Vector3> corners = stackalloc Vector3[8];

        var nearFar = new Vector2(_projInfo.Near, MathF.Min(_projInfo.Far, _projInfo.Near + shadow.Distance));
        var tan = new Vector2(1f / _projectionMatrix.M11, 1f / _projectionMatrix.M22);
        FrustumMath.FillFrustumCorners(in _viewMatrix, _transform.Translation, tan, nearFar, corners);
        CameraUtils.CreateLightView(ref _lightMatrices, in shadow, lightDirection, corners);
    }

    private void Ensure()
    {
        if (!_dirty) return;
        _dirty = false;

        ref var viewMatrix = ref _viewMatrix;
        MatrixMath.CreateFixedSizeModelMatrix(
            in _transform.Translation,
            RotationMath.YawPitchToQuaternion(_transform.Orientation),
            out viewMatrix);

        Matrix4x4.Invert(viewMatrix, out viewMatrix);
        Matrix4x4.Invert(viewMatrix, out var invView);

        _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            FloatMath.ToRadians(_projInfo.Fov * 0.5f),
            _viewport.AspectRatio,
            _projInfo.Near,
            _projInfo.Far
        );

        Matrix4x4.Invert(_projectionMatrix, out var invProjection);
        _invProjectionViewMatrix = invProjection * invView;
        _projectionViewMatrix = viewMatrix * _projectionMatrix;
        Generation++;
    }
}