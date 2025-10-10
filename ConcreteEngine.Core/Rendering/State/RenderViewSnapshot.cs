using System.Numerics;
using ConcreteEngine.Core.Rendering.Data;

namespace ConcreteEngine.Core.Rendering.State;

public readonly struct RenderViewSnapshot(
    in Matrix4x4 viewMatrix,
    in Matrix4x4 projectionMatrix,
    in Matrix4x4 projectionViewMatrix,
    in ProjectionInfo projectionInfo,
    Vector3 position,
    Vector3 forward,
    Vector3 right,
    Vector3 up
)
{
    public readonly Matrix4x4 ViewMatrix = viewMatrix;
    public readonly Matrix4x4 ProjectionMatrix = projectionMatrix;
    public readonly Matrix4x4 ProjectionViewMatrix = projectionViewMatrix;
    public readonly ProjectionInfo ProjectionInfo = projectionInfo;
    public readonly Vector3 Position = position;
    public readonly Vector3 Forward = forward;
    public readonly Vector3 Right = right;
    public readonly Vector3 Up = up;
}
