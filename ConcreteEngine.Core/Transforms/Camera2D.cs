using System.Numerics;
using ConcreteEngine.Core.Transforms;
using Silk.NET.Maths;

namespace ConcreteEngine.Core;

public interface ICamera
{
    public ViewTransform2D Transform { get; }
    public ViewTransform2D RenderTransform { get; }
}

public sealed class Camera2D : ICamera
{
    private readonly ViewTransform2D _transform;
    private readonly ViewTransform2D _renderTransform;

    public ViewTransform2D Transform => _transform;
    public ViewTransform2D RenderTransform => _renderTransform;

    internal Camera2D()
    {
        _transform = new()
        {
            Position = Vector2.Zero,
            Rotation = 0f,
            Zoom = 1f
        };
        _renderTransform = new ViewTransform2D();
        _renderTransform.CopyFrom(Transform);
    }

    public void SetViewport(Vector2D<int> viewport)
    {
        _transform.ViewportSize = viewport;
    }

    internal void PrepareRender()
    {
        _renderTransform.CopyFrom(_transform);
    }
}

