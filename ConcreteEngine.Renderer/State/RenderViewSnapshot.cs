using System.Numerics;
using ConcreteEngine.Shared.World;

namespace ConcreteEngine.Renderer.State;

public struct RenderViewSnapshot(
    in Matrix4x4 viewMatrix,
    in Matrix4x4 projectionMatrix,
    in Matrix4x4 projectionViewMatrix,
    in ViewTransform transform,
    in ProjectionInfo projectionInfo)
{
    public Matrix4x4 ViewMatrix = viewMatrix;
    public Matrix4x4 ProjectionMatrix = projectionMatrix;
    public Matrix4x4 ProjectionViewMatrix = projectionViewMatrix;

    public ViewTransform Transform = transform;
    public ProjectionInfo ProjectionInfo = projectionInfo;

    public readonly Vector3 Right => new(ViewMatrix.M11, ViewMatrix.M21, ViewMatrix.M31);
    public readonly Vector3 Up => new Vector3(ViewMatrix.M12, ViewMatrix.M22, ViewMatrix.M32);
    public readonly Vector3 Forward => -new Vector3(ViewMatrix.M13, ViewMatrix.M23, ViewMatrix.M33);
}