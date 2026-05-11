using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Input;
using Silk.NET.Input;

namespace ConcreteEngine.Editor.Core;

internal sealed class EditorCamera
{
    private const float BaseSpeed = 65f;
    private const float RotationSpeed = 165f;

    public static readonly EditorCamera Instance = new();

    private Vector3 _currentVelocity;
    private YawPitch _targetOrientation;

    private readonly InputController _input = EditorInput.Input;
    public readonly Camera Camera = EngineObjectStore.Camera;

    private EditorCamera()
    {
    }

    public void Update(float dt)
    {
        if (EditorInput.IsBlockingKeyboard) return;
        MovementController(dt, BaseSpeed);
        RotateController(dt, RotationSpeed);
    }

    private void MovementController(float dt, float speed)
    {
        const float acceleration = 12.0f;
        const float friction = 12.0f;

        Vector3 targetVelocity = default;

        if (_input.IsKeyDown(Key.W))
            targetVelocity += Camera.Forward;
        if (_input.IsKeyDown(Key.S))
            targetVelocity -= Camera.Forward;

        if (targetVelocity.LengthSquared() > 0)
            targetVelocity = Vector3.Normalize(targetVelocity) * speed;

        float t = 1.0f - MathF.Exp(-acceleration * dt);
        if (targetVelocity == Vector3.Zero) t = 1.0f - MathF.Exp(-friction * dt);
        _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, t);
        Camera.Translation += _currentVelocity * dt;
    }

    private void RotateController(float fixedDt, float rotateSpeed)
    {
        var speed = rotateSpeed * fixedDt;

        if (!YawPitch.NearlyEqual(Camera.Orientation, _targetOrientation))
            _targetOrientation = Camera.Orientation;

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
        Camera.Orientation = YawPitch.Lerp(Camera.Orientation, _targetOrientation, t);
    }
}