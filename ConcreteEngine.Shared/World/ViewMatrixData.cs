using System.Numerics;

namespace ConcreteEngine.Shared.World;

public struct ViewMatrixData(
    in Matrix4x4 viewMatrix,
    in Matrix4x4 projectionMatrix,
    in Matrix4x4 projectionViewMatrix
)
{
    public Matrix4x4 ViewMatrix = viewMatrix;
    public Matrix4x4 ProjectionMatrix = projectionMatrix;
    public Matrix4x4 ProjectionViewMatrix = projectionViewMatrix;
}