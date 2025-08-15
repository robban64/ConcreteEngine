using ConcreteEngine.Core.Input;
using ConcreteEngine.Graphics.Data;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Transforms;

public sealed class CameraSystem: IGameEngineSystem
{
    private const int EdgeMarginPixels = 16;
    private const float BaseSpeed = 100; //= 900;

    private readonly InputSystem _input;
    private readonly ViewTransform2D _transform = new()         {
        Position = Vector2D<float>.Zero,
        Rotation = 0f,
        Zoom = 1f
    };
    
    public ViewTransform2D Transform => _transform;
    
    internal CameraSystem(InputSystem input)
    {
        _input = input;
    }

    public void Update(in GraphicsFrameContext frameCtx)
    {
        _transform.ViewportSize = frameCtx.FramebufferSize;
        
        float speed = frameCtx.DeltaTime * BaseSpeed;

        var input = _input;
        
        var deltaPos = Vector2D<float>.Zero;

        if (input.IsKeyDown(Key.W))
            deltaPos.Y -= 1f;

        if (input.IsKeyDown(Key.S))
            deltaPos.Y += 1f;

        if (input.IsKeyDown(Key.A))
            deltaPos.X -= 1f;

        if (input.IsKeyDown(Key.D))
            deltaPos.X += 1f;


        var mouse = input.MousePosition;
        var sceeenSize = frameCtx.FramebufferSize;
        var w = sceeenSize.X - EdgeMarginPixels;
        var h =  sceeenSize.Y - EdgeMarginPixels;
        
        // Left / Right
        if (mouse.X <= EdgeMarginPixels) deltaPos.X -= 1f;
        else if (mouse.X >= w) deltaPos.X += 1f;

        // Top / Bottom (note: top-left origin in window coords)
        if (mouse.Y <= EdgeMarginPixels)        deltaPos.Y -= 1f;
        else if (mouse.Y >= h) deltaPos.Y += 1f;
        
        // Normalize so diagonals aren’t faster.
        var len = MathF.Sqrt(deltaPos.X * deltaPos.X + deltaPos.Y * deltaPos.Y);
        if (len > 0f)
        {
            deltaPos /= len;
            _transform.Position += deltaPos * speed;
        }
    }

    public void TickUpdate()
    {
    }

    public void Dispose()
    {
    }

}