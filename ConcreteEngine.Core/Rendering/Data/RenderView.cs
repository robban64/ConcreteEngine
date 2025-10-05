using System.Numerics;
using ConcreteEngine.Core.Scene;

namespace ConcreteEngine.Core.Rendering.Data;

public sealed class RenderView
{
    private Matrix4x4 _viewMatrix;
    private Matrix4x4 _projectionMatrix;
    private Matrix4x4 _projectionViewMatrix;

    private Vector3 _viewPosition;

    public ref readonly Matrix4x4 ViewMatrix => ref _viewMatrix;
    public ref readonly Matrix4x4 ProjectionMatrix => ref _projectionMatrix;
    public ref readonly Matrix4x4 ProjectionViewMatrix => ref _projectionViewMatrix;
    public Vector3 ViewPosition => _viewPosition;

    public RenderView()
    {
    }

    public RenderView(
        in Matrix4x4 viewMatrix,
        in Matrix4x4 projectionMatrix,
        in Matrix4x4 projViewMat,
        Vector3 viewPosition)
    {
        _viewMatrix = viewMatrix;
        _projectionMatrix = projectionMatrix;
        _projectionViewMatrix = projViewMat;
        _viewPosition = viewPosition;
    }

    internal void ApplyCameraView(Camera3D camera)
    {
        _viewMatrix = camera.ViewMatrix;
        _projectionMatrix = camera.ProjectionMatrix;
        _projectionViewMatrix = camera.ProjectionViewMatrix;
        _viewPosition = camera.Translation;
    }

    internal void ApplyLightView(Vector3 direction, Camera3D camera, float orthoHalfSize = 20f, float distance = 60f,
        float zNear = 0.1f, float zFar = 120f)
    {
        var dir = Vector3.Normalize(direction);
        var center = camera.Translation + camera.Forward * (distance * 0.5f);
        var eye = center - dir * distance;

        var worldUp = Math.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;
        _viewMatrix = Matrix4x4.CreateLookAt(eye, center, worldUp);
        _projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(
            -orthoHalfSize, +orthoHalfSize,
            -orthoHalfSize, +orthoHalfSize,
            zNear, zFar);
        _projectionViewMatrix = _viewMatrix * _projectionMatrix;
    }
}