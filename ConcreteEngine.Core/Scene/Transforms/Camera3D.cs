#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Data;

#endregion

namespace ConcreteEngine.Core.Scene;

public sealed class Camera3D : ICamera
{
    private bool _dirty = true;

    private float _yaw = 0;
    private float _pitch = 0;

    private Vector3 _translation = Vector3.Zero;
    private Vector3 _scale = Vector3.One;
    private Quaternion _rotation = Quaternion.Identity;
    private Matrix4x4 _viewMatrix = Matrix4x4.Identity;
    private Matrix4x4 _projectionMatrix = Matrix4x4.Identity;
    private Matrix4x4 _projectionViewMatrix = Matrix4x4.Identity;

    private Size2D _viewportSize;

    private float _fov = 70;
    private float _farPlane = 2000;
    private float _nearPlane = 0.2f;
    private float _aspectRatio;


    public Vector3 Right => Vector3.Normalize(Vector3.Transform(Vector3.UnitX, _rotation));
    public Vector3 Up => Vector3.Normalize(Vector3.Transform(Vector3.UnitY, _rotation));
    public Vector3 Forward => Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, _rotation));


    public float Yaw
    {
        get => _yaw;
        set
        {
            _yaw = value;
            _dirty = true;
        }
    }

    public float Pitch
    {
        get => _pitch;
        set
        {
            _pitch = value;
            _dirty = true;
        }
    }


    public Vector3 Translation
    {
        get => _translation;
        set
        {
            _translation = value;
            _dirty = true;
        }
    }

    public Vector3 Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            _dirty = true;
        }
    }

    public Size2D Viewport
    {
        get => _viewportSize;
        set
        {
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
            _fov = value;
            _dirty = true;
        }
    }

    public float FarPlane
    {
        get => _farPlane;
        set
        {
            _farPlane = value;
            _dirty = true;
        }
    }

    public float NearPlane
    {
        get => _nearPlane;
        set
        {
            _nearPlane = value;
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

    private void Ensure()
    {
        if (!_dirty) return;
        _dirty = false;

        _rotation = Quaternion.CreateFromYawPitchRoll(_yaw, _pitch, 0);
        TransformUtils.CreateModelMatrix(_translation, _scale, _rotation, out var viewModel);
        Matrix4x4.Invert(viewModel, out _viewMatrix);

        var fov = MathHelper.ToRadians(_fov / 2f);
        _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(fov, _aspectRatio, _nearPlane, _farPlane);
        _projectionViewMatrix = _viewMatrix * _projectionMatrix;
    }

    internal void MakeRenderViewInfo(out RenderTickViewInfo viewInfo)
    {
        Ensure();
        var projInfo = new ProjectionInfo(_aspectRatio, _fov, _nearPlane, _farPlane);
        viewInfo = new RenderTickViewInfo(
            viewMatrix: in _viewMatrix,
            projectionMatrix: in _projectionMatrix,
            projectionInfo: in projInfo,
            position: _translation,
            forward: Forward,
            right: Right,
            up: Up
        );
    }
}