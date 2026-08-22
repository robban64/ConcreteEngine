using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using Silk.NET.Input;

namespace ConcreteEngine.Editor.Core;

internal static class EditorCamera
{
    private const float BaseSpeed = 65f;
    private const float RotationSpeed = 165f;

    private static Camera Camera => CameraManager.Instance.Camera;

    private static Vector3 _currentVelocity;
    private static YawPitch _targetOrientation;


    public static void Update(float dt)
    {
        if (EditorInput.IsBlockingKeyboard) return;
        MovementController(dt, BaseSpeed);
        RotateController(dt, RotationSpeed);
    }

    private static void MovementController(float dt, float speed)
    {
        const float acceleration = 12.0f;
        const float friction = 12.0f;

        Vector3 targetVelocity = default;

        if (EditorInput.Layer.IsKeyDown(Key.W))
            targetVelocity += Camera.Forward;
        if (EditorInput.Layer.IsKeyDown(Key.S))
            targetVelocity -= Camera.Forward;

        if (targetVelocity.LengthSquared() > 0)
            targetVelocity = Vector3.Normalize(targetVelocity) * speed;

        float t = 1.0f - MathF.Exp(-acceleration * dt);
        if (targetVelocity == Vector3.Zero) t = 1.0f - MathF.Exp(-friction * dt);
        _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, t);
        Camera.Translation += _currentVelocity * dt;
    }

    private static void RotateController(float fixedDt, float rotateSpeed)
    {
        var speed = rotateSpeed * fixedDt;

        var target = _targetOrientation;

        if (!YawPitch.NearlyEqual(Camera.Orientation, _targetOrientation))
            target = Camera.Orientation;

        if (EditorInput.Layer.IsKeyDown(Key.A))
            target.Yaw += speed;
        if (EditorInput.Layer.IsKeyDown(Key.D))
            target.Yaw += -speed;
        if (EditorInput.Layer.IsKeyDown(Key.Q))
            target.Pitch += speed;
        if (EditorInput.Layer.IsKeyDown(Key.E))
            target.Pitch += -speed;

        target.WithClampedPitch();
        
        float t = 1.0f - MathF.Exp(-25 * fixedDt);
        Camera.Orientation = YawPitch.Lerp(Camera.Orientation, target, t);
        _targetOrientation = target;
    }
}