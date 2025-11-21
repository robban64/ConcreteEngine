#region

using System.Numerics;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Renderer.State;

public struct RenderViewSnapshot(
    in Matrix4x4 viewMatrix,
    in Matrix4x4 projectionMatrix,
    in Matrix4x4 projectionViewMatrix,
    in ProjectionInfoData projectionInfo,
    in Vector3 position,
    in Quaternion rotation
)
{
    public Matrix4x4 ViewMatrix = viewMatrix;
    public Matrix4x4 ProjectionMatrix = projectionMatrix;
    public Matrix4x4 ProjectionViewMatrix = projectionViewMatrix;

    public ProjectionInfoData ProjectionInfo = projectionInfo;

    public Quaternion Rotation = rotation;
    public Vector3 Position = position;

    public readonly Vector3 Right => Vector3.Normalize(Vector3.Transform(Vector3.UnitX, Rotation));
    public readonly Vector3 Up => Vector3.Normalize(Vector3.Transform(Vector3.UnitY, Rotation));
    public readonly Vector3 Forward => Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, Rotation));
}