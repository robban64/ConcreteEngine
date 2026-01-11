using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Worlds;
using Silk.NET.Input;

namespace ConcreteEngine.Engine.Scene.Modules;

public sealed class FlyCameraModule : GameModule
{
    private const float BaseSpeed = 65f;
    private const float RotationSpeed = 165f;

    private Camera _camera = null!;
    private InputLayer _input = null!;

    private Vector3 _currentVelocity;
    private YawPitch _targetOrientation;

    public override void OnStart()
    {
        _input = Context.GetSystem<InputSystem>().GetLayer(InputLayerKind.Game);
        _camera = Context.World.Camera;
    }

    public override void UpdateTick(float dt)
    {
        MovementController(dt, BaseSpeed);
        RotateController(dt, RotationSpeed);
    }

    private void MovementController(float dt, float speed)
    {
        float acceleration = 12.0f;
        float friction = 12.0f;

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

    private void RotateController(float fixedDt, float rotateSpeed)
    {
        var speed = rotateSpeed * fixedDt;

        if (!YawPitch.NearlyEqual(_camera.Orientation, _targetOrientation))
            _targetOrientation = _camera.Orientation;

        var target = _targetOrientation;

        if (_input.IsKeyDown(Key.A))
            target.Yaw += speed;
        if (_input.IsKeyDown(Key.D))
            target.Yaw += -speed;
        if (_input.IsKeyDown(Key.Q))
            target.Pitch += speed;
        if (_input.IsKeyDown(Key.E))
            target.Pitch += -speed;


        target.WithClampedPitch();

        _targetOrientation = target;

        float t = 1.0f - MathF.Exp(-25 * fixedDt);
        _camera.Orientation = YawPitch.Lerp(_camera.Orientation, _targetOrientation, t);
    }
}