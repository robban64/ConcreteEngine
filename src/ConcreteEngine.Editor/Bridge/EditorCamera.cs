using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Renderer;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Silk.NET.Input;

namespace ConcreteEngine.Editor.Bridge;

public sealed class EditorCamera
{
    private const float BaseSpeed = 65f;
    private const float RotationSpeed = 165f;

    public static readonly EditorCamera Instance = new();

    private Vector3 _currentVelocity;
    private YawPitch _targetOrientation;

    private readonly InputController _input = EditorInputState.Input;
    public readonly CameraTransform Camera = EngineObjectStore.Camera;
    
    public EditorCamera()
    {
    }

    public void Update(float dt)
    {
        if (EditorInputState.IsBlockingKeyboard) return;
        MovementController(dt, BaseSpeed);
        RotateController(dt, RotationSpeed);
    }

    public unsafe void DrawGizmos(bool enabled, InspectSceneObject inspector)
    {
        Matrix4x4* matrices = stackalloc Matrix4x4[3];
        var view = &matrices[0];
        var proj = &matrices[1];
        var model = &matrices[2];

        *view = Camera.ViewMatrix;
        *proj = Camera.ProjectionMatrix;
        MatrixMath.CreateModelMatrix(in inspector.SceneObject.GetTransform(), out *model);

        ImGuizmo.Enable(enabled);
        var changed = ImGuizmo.Manipulate(
            &view->M11,
            &proj->M11,
            EditorInputState.GizmoOperation,
            EditorInputState.GizmoMode,
            &model->M11
        );

        if (changed && enabled)
        {
            Transform.FromMatrix(in *model, out var transform);
            inspector.SceneObject.SetTransform(in transform);
        }
    }

    private void MovementController(float dt, float speed)
    {
        float acceleration = 12.0f;
        float friction = 12.0f;

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