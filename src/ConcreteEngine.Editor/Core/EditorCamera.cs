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
    private const float RotationSpeed = 165.0f;
    
    private static Vector3 _currentVelocity;
    private static Vector2 _targetOrientation;


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

        var camera = Camera.Main;

        Vector3 targetVelocity = default;

        if (EditorInput.Layer.IsKeyDown(Key.W))
            targetVelocity += camera.Forward;
        if (EditorInput.Layer.IsKeyDown(Key.S))
            targetVelocity -= camera.Forward;

        if (targetVelocity.LengthSquared() > 0)
            targetVelocity = Vector3.Normalize(targetVelocity) * speed;

        float t = 1.0f - MathF.Exp(-acceleration * dt);
        if (targetVelocity == Vector3.Zero) t = 1.0f - MathF.Exp(-friction * dt);
        
        _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, t);
        camera.Translation += _currentVelocity * dt;
    }

    private static void RotateController(float fixedDt, float rotateSpeed)
    {
        var speed = rotateSpeed * fixedDt;

        var camera = Camera.Main;
        var target = _targetOrientation;

        if (!VectorMath.NearlyEqual(camera.Orientation, _targetOrientation))
            target = camera.Orientation;
        
        if (EditorInput.Layer.IsKeyDown(Key.A))
            target.X += speed;
        if (EditorInput.Layer.IsKeyDown(Key.D))
            target.X += -speed;
        if (EditorInput.Layer.IsKeyDown(Key.Q))
            target.Y += speed;
        if (EditorInput.Layer.IsKeyDown(Key.E))
            target.Y += -speed;

        target.Y = RotationMath.ClampPitch(target.Y);

        float t = 1.0f - float.Exp(-25 * fixedDt);
        camera.Orientation = Vector2.Lerp(camera.Orientation, target, t);
        _targetOrientation = target;
    }
}