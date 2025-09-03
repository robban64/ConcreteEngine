using System.Numerics;
using ConcreteEngine.Common;
using Silk.NET.Maths;

namespace ConcreteEngine.Core;

public sealed class Camera3D : ICamera
{
    private bool _dirty = true;

    private Vector3 _translation = Vector3.Zero;
    private Vector3 _scale = Vector3.One;
    private Quaternion _rotation = Quaternion.Identity;
    private Matrix4x4 _viewMatrix = Matrix4x4.Identity;
    private Matrix4x4 _projectionMatrix = Matrix4x4.Identity;
    private Matrix4x4 _projectionViewMatrix = Matrix4x4.Identity;

    private Vector2D<int> _viewportSize;

    private float _fov = 70;
    private float _farPlane = 2000;
    private float _nearPlane = 0.2f;
    private float _aspectRatio;

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

    public Quaternion Identity
    {
        get => _rotation;
        set
        {
            _rotation = value;
            _dirty = true;
        }
    }

    public Vector2D<int> ViewportSize
    {
        get => _viewportSize;
        set
        {
            _viewportSize = value;
            _aspectRatio = _viewportSize.X / (float)_viewportSize.Y;
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

        _viewMatrix = Matrix4x4.Identity *
                      Matrix4x4.CreateFromQuaternion(_rotation) *
                      Matrix4x4.CreateScale(_scale) *
                      Matrix4x4.CreateTranslation(_translation);

        var fov = MathHelper.ToRadians(_fov / 2f);
        _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(fov, _aspectRatio, _nearPlane, _farPlane);
        _projectionViewMatrix = _viewMatrix * _projectionMatrix;
    }
}