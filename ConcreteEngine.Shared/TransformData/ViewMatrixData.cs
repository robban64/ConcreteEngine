#region

using System.Numerics;

#endregion

namespace ConcreteEngine.Shared.TransformData;

public struct ViewMatrixData(
    in Matrix4x4 modelMatrix,
    in Matrix4x4 projectionMatrix,
    in Matrix4x4 projectionViewMatrix
)
{
    public Matrix4x4 ModelMatrix = modelMatrix;
    public Matrix4x4 ProjectionMatrix = projectionMatrix;
    public Matrix4x4 ProjectionViewMatrix = projectionViewMatrix;
}