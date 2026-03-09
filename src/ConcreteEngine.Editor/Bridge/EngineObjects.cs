using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Renderer;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Silk.NET.Input;

namespace ConcreteEngine.Editor.Bridge;

internal static class EngineObjects
{
    public static CameraTransform Camera = null!;
    public static VisualEnvironment Visuals = null!;
}

public sealed class EditorCamera
{
    private const float BaseSpeed = 65f;
    private const float RotationSpeed = 165f;

    public static readonly EditorCamera Instance = new();

    private Vector3 _currentVelocity;
    private YawPitch _targetOrientation;

    public void Update(float dt)
    {
        if(EngineObjects.Camera is not {} camera) return;
        MovementController(camera,dt, BaseSpeed);
        RotateController(camera,dt, RotationSpeed);
    }

    public unsafe void DrawGizmos(bool enabled, InspectSceneObject inspector)
    {
        Matrix4x4* matrices = stackalloc Matrix4x4[3];
        var view = &matrices[0];
        var proj = &matrices[1];
        var model = &matrices[2];

        *view = EngineObjects.Camera.ViewMatrix;
        *proj = EngineObjects.Camera.ProjectionMatrix;
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

    private void MovementController(CameraTransform camera, float dt, float speed)
    {
        float acceleration = 12.0f;
        float friction = 12.0f;

        Vector3 targetVelocity = default;

        if (ImGui.IsKeyDown(ImGuiKey.W))
            targetVelocity += camera.Forward;
        if (ImGui.IsKeyDown(ImGuiKey.S))
            targetVelocity -= camera.Forward;

        if (targetVelocity.LengthSquared() > 0)
            targetVelocity = Vector3.Normalize(targetVelocity) * speed;

        float t = 1.0f - MathF.Exp(-acceleration * dt);
        if (targetVelocity == Vector3.Zero) t = 1.0f - MathF.Exp(-friction * dt);
        _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, t);
        camera.Translation += _currentVelocity * dt;
    }

    private void RotateController(CameraTransform camera, float fixedDt, float rotateSpeed)
    {
        var speed = rotateSpeed * fixedDt;

        if (!YawPitch.NearlyEqual(camera.Orientation, _targetOrientation))
            _targetOrientation = camera.Orientation;

        var target = _targetOrientation;

        if (ImGui.IsKeyDown(ImGuiKey.A))
            target.Yaw += speed;
        if (ImGui.IsKeyDown(ImGuiKey.D))
            target.Yaw += -speed;
        if (ImGui.IsKeyDown(ImGuiKey.Q))
            target.Pitch += speed;
        if (ImGui.IsKeyDown(ImGuiKey.E))
            target.Pitch += -speed;


        target.WithClampedPitch();

        _targetOrientation = target;

        float t = 1.0f - MathF.Exp(-25 * fixedDt);
        camera.Orientation = YawPitch.Lerp(camera.Orientation, _targetOrientation, t);
    }
}