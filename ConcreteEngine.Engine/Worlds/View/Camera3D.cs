#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Renderer.State;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Engine.Worlds.View;

// TODO improve
public sealed class Camera3D : ICamera
{
    private const float MinNearPlane = 0.1f;
    private const float MaxNearPlane = 4f;

    private const float MinFarPlane = 5f;
    private const float MaxFarPlane = 10_000f;

    private const float MinFov = 10;
    private const float MaxFov = 180;

    private const float DirtyThreshold = MetricUnits.Millimeter;

    private bool _dirty;

    private CameraTransformData _prevTick;
    private CameraTransformData _currTick;

    private YawPitch _orientation;

    private Vector3 _translation = Vector3.Zero;
    private Vector3 _scale = Vector3.One;
    private Quaternion _rotation = Quaternion.Identity;
    private Matrix4x4 _viewMatrix = Matrix4x4.Identity;
    private Matrix4x4 _projectionMatrix = Matrix4x4.Identity;
    private Matrix4x4 _projectionViewMatrix = Matrix4x4.Identity;

    private Size2D _viewportSize;

    private float _fov = 70;
    private float _farPlane = 1000;
    private float _nearPlane = 0.1f;
    private float _aspectRatio;

    public long Generation { get; private set; } = 0;

    private CameraRaycaster _raycaster;

    public Camera3D()
    {
        Ensure();
        _dirty = true;
        _raycaster = new CameraRaycaster();
    }

    public Vector3 Right => Vector3.Normalize(Vector3.Transform(Vector3.UnitX, _rotation));
    public Vector3 Up => Vector3.Normalize(Vector3.Transform(Vector3.UnitY, _rotation));
    public Vector3 Forward => Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, _rotation));

    public YawPitch Orientation
    {
        get => _orientation;
        set
        {
            if (YawPitch.NearlyEqual(value, _orientation)) return;
            _orientation = value;
            _dirty = true;
        }
    }

    public Vector3 Translation
    {
        get => _translation;
        set
        {
            if (VectorMath.DistanceNearlyEqual(in value, in _translation, DirtyThreshold)) return;
            _translation = value;
            _dirty = true;
        }
    }

    public Vector3 Scale
    {
        get => _scale;
        set
        {
            if (VectorMath.DistanceNearlyEqual(in value, in _scale, DirtyThreshold)) return;
            _scale = value;
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
            _aspectRatio = value.AspectRatio;
            _dirty = true;
        }
    }

    public float Fov
    {
        get => _fov;
        set
        {
            if (FloatMath.NearlyEqual(value, _fov, MetricUnits.Decimeter)) return;
            _fov = float.Clamp(value, MinFov, MaxFov);
            _dirty = true;
        }
    }

    public float FarPlane
    {
        get => _farPlane;
        set
        {
            if (FloatMath.NearlyEqual(value, _farPlane, MetricUnits.Millimeter)) return;
            _farPlane = float.Min(float.Max(value, MinFarPlane), MaxFarPlane);
            _dirty = true;
        }
    }

    public float NearPlane
    {
        get => _nearPlane;
        set
        {
            if (FloatMath.NearlyEqual(value, _nearPlane, MetricUnits.Millimeter)) return;
            _nearPlane = float.Min(float.Max(value, MinNearPlane), MaxNearPlane);
            _dirty = true;
        }
    }

    public float AspectRatio => _aspectRatio;


    public Quaternion Rotation
    {
        get
        {
            Ensure();
            return _rotation;
        }
    }

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

    internal void EndTick()
    {
        Ensure();
        _prevTick = _currTick;
        CameraTransformData.FromCamera(this, out _currTick);
    }

    internal void WriteSnapshot(float alpha, ref RenderViewSnapshot viewSnapshot)
    {
        var camPos = Vector3.Lerp(_prevTick.Translation, _currTick.Translation, alpha);
        var camRot = Quaternion.Slerp(_prevTick.Rotation, _currTick.Rotation, alpha);
        MatrixMath.CreateModelMatrix(camPos, _scale, camRot, out var viewMatrix);
        Matrix4x4.Invert(viewMatrix, out viewMatrix);

        viewSnapshot.ViewMatrix = viewMatrix;
        viewSnapshot.ProjectionMatrix = _projectionMatrix;
        viewSnapshot.ProjectionViewMatrix = viewMatrix * _projectionMatrix;
        viewSnapshot.ProjectionInfo = new ProjectionInfoData(_aspectRatio, _fov, _nearPlane, _farPlane);
        viewSnapshot.Position = camPos;
        viewSnapshot.Rotation = camRot;
    }

    private void Ensure()
    {
        if (!_dirty) return;
        _dirty = false;

        _orientation.ToQuaternion(out _rotation);

        MatrixMath.CreateModelMatrix(_translation, _scale, _rotation, out var viewModel);
        Matrix4x4.Invert(viewModel, out _viewMatrix);

        var fov = FloatMath.ToRadians(_fov / 2f);
        _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(fov, _aspectRatio, _nearPlane, _farPlane);
        _projectionViewMatrix = _viewMatrix * _projectionMatrix;

        Generation++;
    }
}