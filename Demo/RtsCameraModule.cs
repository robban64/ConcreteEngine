#region

using System.Numerics;
using ConcreteEngine.Core;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics.Data;
using Silk.NET.Input;

#endregion

namespace Demo;

public class RtsCameraModule : GameModule
{
    private const int EdgeMarginPixels = 16;
    private const float BaseSpeed = 200;

    private IGameCamera _camera;
    private IEngineInputSource _input;

    public override void Initialize()
    {
        _camera = Context.GetSystem<IRenderSystem>().Camera;
        _input = Context.GetSystem<IInputSystem>().InputSource;
    }

    public override void Update(in FrameMetaInfo frameCtx)
    {
        var _transform = _camera.Transform;

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

    public override void UpdateTick(int tick)
    {
    }
}