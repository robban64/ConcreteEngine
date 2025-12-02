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
    private const float BaseSpeed = 65f;
    private const float RotationSpeed = 65f;

    private Camera3D _camera = null!;
    private IEngineInputSource _input = null!;

    private Vector3 _currentVelocity;
    private YawPitch _targetOrientation;

    public override void Initialize()
    {
        _input = Context.GetSystem<IInputSystem>().InputSource;
        _camera = Context.World.Camera;
    }


    public override void Update(in UpdateTickInfo frameCtx)
    {
    }


    public override void UpdateTick(int tick, float fixedDt)
    {
        MovementController(fixedDt, BaseSpeed);
        RotateController(fixedDt, RotationSpeed);
    }

    private void MovementController(float dt, float speed)
    {
        float acceleration = 8.0f;
        float friction = 8.0f;

        Vector3 targetVelocity = default;

        if (_input.IsKeyDown(Key.W))
            targetVelocity += _camera.Forward;
        if (_input.IsKeyDown(Key.S))
            targetVelocity -= _camera.Forward;

        if (targetVelocity.LengthSquared() > 0)
            targetVelocity = Vector3.Normalize(targetVelocity) * speed;

        float t = 1.0f - MathF.Exp(-acceleration * dt);
        if (targetVelocity == Vector3.Zero) t = 1.0f - MathF.Exp(-friction * dt);
        _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, t);
        _camera.Translation += _currentVelocity * dt;
    }

    private void RotateController(float dt, float rotateSpeed)
    {
        var speed = rotateSpeed * dt;

        var orientation = _targetOrientation;
        if (_input.IsKeyDown(Key.A))
            orientation.Yaw += speed;
        if (_input.IsKeyDown(Key.D))
            orientation.Yaw += (-speed);
        if (_input.IsKeyDown(Key.Q))
            orientation.Pitch += (speed);
        if (_input.IsKeyDown(Key.E))
            orientation.Pitch += (-speed);

        float t = 1.0f - MathF.Exp(-10 * dt);
        orientation.WithClampedPitch();
        _targetOrientation = orientation;
        _camera.Orientation = YawPitch.Lerp(_camera.Orientation, _targetOrientation, t);
    }
}