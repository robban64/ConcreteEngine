#region

using System.Numerics;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Transforms;

public sealed class ViewTransform2D
{
    private bool _dirty = true;

    private Vector2 _position = Vector2.Zero;
    private float _rotation = 0f;
    private float _zoom = 1f;
    private Vector2D<int> _viewportSize;

    private Matrix4x4 _projectionMat, _viewMat;

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

    public Vector2D<int> ViewportSize
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
            return _viewMat;
        }
    }

    public Matrix4x4 ProjectionMatrix
    {
        get
        {
            Ensure();
            return _projectionMat;
        }
    }

    public Matrix4x4 ProjectionViewMatrix
    {
        get
        {
            Ensure();
            return _viewMat * _projectionMat;
        }
    }

    /// <summary>Snap camera to pixel grid (world step = 1/Zoom).</summary>
    public bool PixelSnap { get; set; } = true;


    private void Ensure()
    {
        if (!_dirty) return;
        _dirty = false;

        var camPos = _position;

        if (PixelSnap && _zoom > 0f)
        {
            float step = 1f / _zoom; // world units per screen pixel at current zoom
            camPos.X = MathF.Round(camPos.X / step) * step;
            camPos.Y = MathF.Round(camPos.Y / step) * step;
        }

        var translate = Matrix4x4.CreateTranslation(new Vector3(-camPos, 0f));
        var rotate = _rotation != 0f ? Matrix4x4.CreateRotationZ(-_rotation) : Matrix4x4.Identity;
        _viewMat = rotate * translate;

        float w = MathF.Max(_viewportSize.X, 1);
        float h = MathF.Max(_viewportSize.Y, 1);
        float invZ = 1f / _zoom;

        _projectionMat = Matrix4x4.CreateOrthographicOffCenter(
            0f, w * invZ, // left, right
            h * invZ, 0f, // bottom, top (Y-down)
            -1f, 1f
        );

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
}