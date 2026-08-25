using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using Silk.NET.Input;

namespace ConcreteEngine.Editor.Core;

internal static class EditorCamera
{
    private const float BaseSpeed = 65f;
    private const double RotationSpeed = 165.0;

    private static Camera Camera => CameraManager.Instance.Camera;

    private static Vector3 _currentVelocity;
    private static Vector2D _targetOrientation;


    public static void Update(double dt)
    {
        if (EditorInput.IsBlockingKeyboard) return;
        MovementController((float)dt, BaseSpeed);
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

    private static void RotateController(double fixedDt, double rotateSpeed)
    {
        var speed = rotateSpeed * fixedDt;

        var target = _targetOrientation;

        if (!VectorMath.NearlyEqual(Camera.Orientation.AsVector256(), _targetOrientation.AsVector256()))
            target = Camera.Orientation;

        if (EditorInput.Layer.IsKeyDown(Key.A))
            target.X += speed;
        if (EditorInput.Layer.IsKeyDown(Key.D))
            target.X += -speed;
        if (EditorInput.Layer.IsKeyDown(Key.Q))
            target.Y += speed;
        if (EditorInput.Layer.IsKeyDown(Key.E))
            target.Y += -speed;

        target.Y = RotationMath.ClampPitch(target.Y);

        double t = 1.0 - double.Exp(-25 * fixedDt);
        Camera.Orientation = Vector2D.Lerp(Camera.Orientation, target, t);
        _targetOrientation = target;
    }
}