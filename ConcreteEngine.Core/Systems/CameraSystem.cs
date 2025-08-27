#region

using System.Numerics;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Graphics.Data;
using Silk.NET.Input;

#endregion

namespace ConcreteEngine.Core.Transforms;

public sealed class CameraSystem : IGameEngineSystem
{
    private const int EdgeMarginPixels = 16;
    private const float BaseSpeed = 200;

    private readonly IEngineInputSource _input;

    private readonly ViewTransform2D _transform = new()
    {
        Position = Vector2.Zero,
        Rotation = 0f,
        Zoom = 1f
    };

    public ViewTransform2D Transform => _transform;

    internal CameraSystem(IEngineInputSource input)
    {
        _input = input;
    }

    public void Update(in FrameMetaInfo frameCtx)
    {
        _transform.ViewportSize = frameCtx.ViewportSize;

        float speed = BaseSpeed * frameCtx.DeltaTime;

        var input = _input;

        var deltaPos = Vector2.Zero;

        if (input.IsKeyDown(Key.W))
            deltaPos.Y -= 1f;

        if (input.IsKeyDown(Key.S))
            deltaPos.Y += 1f;

        if (input.IsKeyDown(Key.A))
            deltaPos.X -= 1f;

        if (input.IsKeyDown(Key.D))
            deltaPos.X += 1f;

        if (input.IsKeyDown(Key.U))
            _transform.Zoom += 0.1f;
        if (input.IsKeyDown(Key.J))
            _transform.Zoom -= 0.1f;

        var mouse = input.MousePosition;
        var w = frameCtx.ViewportSize.X - EdgeMarginPixels;
        var h = frameCtx.ViewportSize.Y - EdgeMarginPixels;

        // Left / Right
        if (mouse.X <= EdgeMarginPixels) deltaPos.X -= 1f;
        else if (mouse.X >= w) deltaPos.X += 1f;

        if (mouse.Y <= EdgeMarginPixels) deltaPos.Y -= 1f;
        else if (mouse.Y >= h) deltaPos.Y += 1f;

        var len = MathF.Sqrt(deltaPos.X * deltaPos.X + deltaPos.Y * deltaPos.Y);
        if (len > 0f)
        {
            deltaPos /= len;
            _transform.Position += deltaPos * speed;
        }
    }

    public void Shutdown()
    {
    }

}