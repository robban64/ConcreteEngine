#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Engine.Worlds.View;

internal readonly ref struct CameraRenderView(
    ref Matrix4x4 viewMatrix,
    ref ProjectionInfo projectionInfo,
    ref BoundingFrustum frustum)
{
    public readonly ref Matrix4x4 ViewMatrix = ref viewMatrix;
    public readonly ref ProjectionInfo ProjectionInfo = ref projectionInfo;
    public readonly ref BoundingFrustum Frustum = ref frustum;
}