#region

using System.Numerics;

#endregion

namespace ConcreteEngine.Shared.TransformData;

public readonly struct TransformMatrixData(
    in Matrix4x4 modelMatrix,
    in Matrix4x4 projectionMatrix,
    in Matrix4x4 projectionViewMatrix
)
{
    public readonly Matrix4x4 ModelMatrix = modelMatrix;
    public readonly Matrix4x4 ProjectionMatrix = projectionMatrix;
    public readonly Matrix4x4 ProjectionViewMatrix = projectionViewMatrix;
}