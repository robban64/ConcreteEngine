#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Core.Scene;

public sealed class Camera2D : ICamera
{
    private bool _dirty = true;

    private Vector2 _position = Vector2.Zero;
    private float _rotation = 0f;
    private float _zoom = 1f;
    private Size2D _viewportSize;

    private Matrix4x4 _viewMatrix = Matrix4x4.Identity;
    private Matrix4x4 _projectionMatrix = Matrix4x4.Identity;
    private Matrix4x4 _projectionViewMatrix = Matrix4x4.Identity;

    public Vector3 Translation => _position.ToVec3();

    public Vector2 Position
    {
        get => _position;
        set
        {
            _position = value;
            _dirty = true;
        }
    }

    public float Rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;
            _dirty = true;
        }
    } // In radians

    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = MathF.Max(Math.Min(value, 4), 0.1f);
            _dirty = true;
        }
    } // >1: zoom in, <1: zoom out

    public Size2D Viewport
    {
        get => _viewportSize;
        set
        {
            _viewportSize = value;
            _dirty = true;
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

    /// <summary>Snap camera to pixel grid (world step = 1/Zoom).</summary>
    public bool PixelSnap { get; set; } = true;


    private void Ensure()
    {
        if (!_dirty) return;
        _dirty = false;

        if (PixelSnap && _zoom > 0f)
        {
            float step = 1f / _zoom; // world units per screen pixel at current zoom
            _position.X = MathF.Round(_position.X / step) * step;
            _position.Y = MathF.Round(_position.Y / step) * step;
        }

        var translate = Matrix4x4.CreateTranslation(new Vector3(-_position, 0f));
        var rotate = _rotation != 0f ? Matrix4x4.CreateRotationZ(-_rotation) : Matrix4x4.Identity;
        _viewMatrix = rotate * translate;

        float w = MathF.Max(_viewportSize.Width, 1);
        float h = MathF.Max(_viewportSize.Height, 1);
        float invZ = 1f / _zoom;

        _projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(
            0f, w * invZ, // left, right
            h * invZ, 0f, // bottom, top (Y-down)
            -1f, 1f
        );

        _projectionViewMatrix = _viewMatrix * _projectionMatrix;

        /*
              float halfW = (ViewportSize.X * 0.5f) / Zoom;
              float halfH = (ViewportSize.Y * 0.5f) / Zoom;
              return Matrix4X4.CreateOrthographicOffCenter(
                 -halfW, halfW,
                 -halfH, halfH,
                 -1f, 1f
             );
           */
    }

    public bool IsAabbInView(Vector2 worldCenter, Vector2 halfExtents)
    {
        // world -> view
        var v = Vector4.Transform(new Vector4(worldCenter, 0f, 1f), _viewMatrix);
        var viewCenter = new Vector2(v.X, v.Y);

        var wv = _viewportSize.Width * (1f / _zoom); // camera width in view space
        var hv = _viewportSize.Height * (1f / _zoom); // camera height in view space

        return !(viewCenter.X + halfExtents.X < 0f ||
                 viewCenter.X - halfExtents.X > wv ||
                 viewCenter.Y + halfExtents.Y < 0f ||
                 viewCenter.Y - halfExtents.Y > hv);
    }

    public RectF GetSimpleViewRect()
    {
        float invZoom = 1f / _zoom;
        float viewWidth = _viewportSize.Width * invZoom;
        float viewHeight = _viewportSize.Height * invZoom;
        return new RectF(_position.X, _position.Y, viewWidth, viewHeight);
    }

    internal void CopyFrom(Camera2D from)
    {
        _position = from.Position;
        _rotation = from.Rotation;
        _zoom = from.Zoom;
        _viewportSize = from.Viewport;
        _dirty = true;
        Ensure();
    }
}