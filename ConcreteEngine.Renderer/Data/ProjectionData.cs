#region

using System.Numerics;

#endregion

namespace ConcreteEngine.Renderer.Data;

public readonly struct ViewTransformData(
    in Matrix4x4 viewMatrix,
    in Matrix4x4 projectionMatrix,
    in Matrix4x4 projectionViewMatrix
)
{
    public readonly Matrix4x4 ViewMatrix = viewMatrix;
    public readonly Matrix4x4 ProjectionMatrix = projectionMatrix;
    public readonly Matrix4x4 ProjectionViewMatrix = projectionViewMatrix;
}

public readonly struct ProjectionInfo(float aspectRatio, float fov, float near, float far)
{
    public float AspectRatio { get; init; } = aspectRatio;
    public float Fov { get; init; } = fov;
    public float Near { get; init; } = near;
    public float Far { get; init; } = far;
}