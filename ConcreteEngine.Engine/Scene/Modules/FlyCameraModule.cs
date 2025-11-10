#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Data;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Worlds.View;
using Silk.NET.Input;

#endregion

namespace ConcreteEngine.Engine.Scene.Modules;

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

        var speed = BaseSpeed;
        var rotateSpeed = RotationSpeed;

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

        _camera.Translation = Vector3.Lerp(_camera.Translation, newPos, dt);
        _camera.Orientation = YawPitch.Lerp(_camera.Orientation, orientation, dt);
    }


    public override void UpdateTick(int tick)
    {
    }
}