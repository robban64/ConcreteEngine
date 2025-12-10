#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Renderer.State;
using ConcreteEngine.Shared.World;

#endregion

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

    private const float DirtyThreshold = MetricUnits.Millimeter;

    private bool _dirty;

    private ViewTransform _prevTransform;
    private ViewTransform _transform = new(Vector3.Zero, Vector3.One, default);

    private Matrix4x4 _viewMatrix = Matrix4x4.Identity;
    private Matrix4x4 _projectionMatrix = Matrix4x4.Identity;
    private Matrix4x4 _projectionViewMatrix = Matrix4x4.Identity;

    private ProjectionInfo _projInfo = new(70, 0.1f, 500);

    private Size2D _viewportSize;


    public long Generation { get; private set; } = 0;

    private readonly CameraRaycaster _raycaster;

    public Camera3D()
    {
        Ensure();
        _dirty = true;
        _raycaster = new CameraRaycaster();
    }

    public Vector3 Right => new(_viewMatrix.M11, _viewMatrix.M21, _viewMatrix.M31);
    public Vector3 Up => new Vector3(_viewMatrix.M12, _viewMatrix.M22, _viewMatrix.M32);
    public Vector3 Forward => -new Vector3(_viewMatrix.M13, _viewMatrix.M23, _viewMatrix.M33);


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

    public Vector3 Scale
    {
        get => _transform.Scale;
        set
        {
            if (VectorMath.DistanceNearlyEqual(in value, in _transform.Scale, DirtyThreshold)) return;
            _transform.Scale = value;
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

    public float AspectRatio => _projInfo.AspectRatio;


    public Matrix4x4 ViewMatrix
    {
        get
        {
            Ensure();
            return _viewMatrix;
        }
    }

    public Matrix4x4 ProjectionMatrix
    {
        get
        {
            Ensure();
            return _projectionMatrix;
        }
    }

    public Matrix4x4 ProjectionViewMatrix
    {
        get
        {
            Ensure();
            return _projectionViewMatrix;
        }
    }

    public CameraRaycaster Raycaster
    {
        get
        {
            if (_raycaster.Generation != Generation)
                _raycaster.UpdateFromCamera(Generation, _viewportSize, in _viewMatrix, in _projectionMatrix);

            return _raycaster;
        }
    }


    // before frame start
    internal void EndTick(RenderParamsSnapshot renderParams, RenderCamera renderCamera)
    {
        Ensure();

        var lightDir = renderParams.SunLight.Direction;
        ref readonly var shadows = ref renderParams.Shadows;
        ref var lightSpace = ref renderCamera.LightSpace;
        var near = _projInfo.Near;
        var far = MathF.Min(_projInfo.Far, near + shadows.Distance);

        Span<Vector3> corners = stackalloc Vector3[8];
        RenderTransform.FillFrustumCorners(corners, in _viewMatrix, in _projectionMatrix,
            _transform.Translation, near, far);

        RenderTransform.CreateLightView(ref lightSpace, corners, lightDir, in shadows);
    }

    internal void WriteSnapshot(float alpha, ref RenderViewSnapshot viewSnapshot)
    {
        var camPos = Vector3.Lerp(_prevTransform.Translation, _transform.Translation, alpha);
        var camOrientation = YawPitch.Lerp(_prevTransform.Orientation, _transform.Orientation, alpha);
        camOrientation.ToQuaternion(out var camRot);
        MatrixMath.CreateFixedSizeModelMatrix(in camPos, in camRot, out var viewMatrix);
        Matrix4x4.Invert(viewMatrix, out viewMatrix);

        ref readonly var projMat = ref _projectionMatrix;
        viewSnapshot.ViewMatrix = viewMatrix;
        viewSnapshot.ProjectionMatrix = projMat;
        viewSnapshot.ProjectionViewMatrix = viewMatrix * projMat;
        viewSnapshot.ProjectionInfo = _projInfo;
        viewSnapshot.Transform = _transform;
    }

    private void Ensure()
    {
        if (!_dirty) return;
        _dirty = false;

        _prevTransform = _transform;

        _transform.Orientation.ToQuaternion(out var rot);

        MatrixMath.CreateFixedSizeModelMatrix(_transform.Translation, in rot, out var viewModel);
        Matrix4x4.Invert(viewModel, out _viewMatrix);

        var fov = FloatMath.ToRadians(_projInfo.Fov / 2f);
        _projectionMatrix =
            Matrix4x4.CreatePerspectiveFieldOfView(fov, _projInfo.AspectRatio, _projInfo.Near, _projInfo.Far);
        _projectionViewMatrix = _viewMatrix * _projectionMatrix;

        Generation++;
    }

    internal void FillData(out CameraDataState data)
    {
        data.Transform = _transform;
        data.Projection = _projInfo;
        data.Viewport = _viewportSize;
    }

    internal void SetFromData(in CameraDataState data)
    {
        _transform = data.Transform;
        _projInfo = data.Projection;
        _dirty = true;
    }
}