using System.Numerics;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Worlds.View;
using Silk.NET.Input;

namespace ConcreteEngine.Core.Scene.Modules;


public sealed class FlyCameraModule : GameModule
{
    private const float PitchLimit = MathF.PI / 2f * 0.99f;

    private const float BaseSpeed = 90;
    private const float RotationSpeed = 60;

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
        var rotateSpeed = RotationSpeed * dt;

        float yaw = _camera.Yaw, pitch = _camera.Pitch;
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

        pitch = Math.Clamp(pitch, -PitchLimit, +PitchLimit);

        _camera.Translation = Vector3.Lerp(_camera.Translation, newPos, dt);
        _camera.Yaw = float.Lerp(_camera.Yaw, yaw, dt);
        _camera.Pitch = float.Lerp(_camera.Pitch, pitch, dt);
    }


    public override void UpdateTick(int tick)
    {
    }
}