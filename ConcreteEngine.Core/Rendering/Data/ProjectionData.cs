using System.Numerics;

namespace ConcreteEngine.Core.Rendering.Data;

public readonly struct ViewProjectionData(
    in Matrix4x4 viewMatrix,
    in Matrix4x4 projectionMatrix,
    in Matrix4x4 projectionViewMatrix
)
{
    public readonly Matrix4x4 ViewMatrix = viewMatrix;
    public readonly Matrix4x4 ProjectionMatrix = projectionMatrix;
    public readonly Matrix4x4 ProjectionViewMatrix = projectionViewMatrix;
}

public readonly record struct ProjectionInfo(float AspectRatio, float Fov, float Near, float Far);