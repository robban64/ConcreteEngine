using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Worlds.View;
using Silk.NET.Input;

namespace ConcreteEngine.Core.Scene.Modules;

public sealed class FlyCameraModule : GameModule
{
    private const float BaseSpeed = 90;
    private const float RotationSpeed = 33f;

    private Camera3D _camera = null!;
    private IEngineInputSource _input = null!;

    public override void Initialize()
    {
        _input = Context.GetSystem<IInputSystem>().InputSource;
    }

    public override void Update(in UpdateTickInfo frameCtx)
    {
        _camera = Context.World.Camera;

        var dt = frameCtx.DeltaTime;

        var speed = BaseSpeed * dt;
        var rotateSpeed = RotationSpeed * dt;

        (float yaw, float pitch) = _camera.Orientation;
        var newPos = _camera.Translation;

        if (_input.IsKeyDown(Key.W))
            newPos += _camera.Forward * speed;
        if (_input.IsKeyDown(Key.S))
            newPos += _camera.Forward * -speed;

        if (_input.IsKeyDown(Key.A))
            yaw += rotateSpeed;
        if (_input.IsKeyDown(Key.D))
            yaw -= rotateSpeed;
        if (_input.IsKeyDown(Key.Q))
            pitch += rotateSpeed;
        if (_input.IsKeyDown(Key.E))
            pitch -= rotateSpeed;

        var orientation = new YawPitch(yaw, pitch).WithClampedPitch();

        _camera.Translation = newPos;
        _camera.Orientation = orientation;
    }


    public override void UpdateTick(int tick)
    {
    }
}