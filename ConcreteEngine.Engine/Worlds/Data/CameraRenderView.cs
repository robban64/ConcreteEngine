using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Shared.World;

namespace ConcreteEngine.Engine.Worlds.Data;

internal readonly ref struct CameraRenderView(
    ref Matrix4x4 viewMatrix,
    ref ProjectionInfo projectionInfo,
    ref BoundingFrustum frustum)
{
    public readonly ref Matrix4x4 ViewMatrix = ref viewMatrix;
    public readonly ref ProjectionInfo ProjectionInfo = ref projectionInfo;
    public readonly ref BoundingFrustum Frustum = ref frustum;
}