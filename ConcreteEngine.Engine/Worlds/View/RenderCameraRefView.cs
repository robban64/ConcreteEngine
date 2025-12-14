using System.Numerics;
using ConcreteEngine.Shared.World;

namespace ConcreteEngine.Engine.Worlds.Render.Data;

internal readonly ref struct RenderCameraRefView(ref Matrix4x4 viewMatrix, ref ProjectionInfo projectionInfo)
{
    public readonly ref Matrix4x4 ViewMatrix = ref viewMatrix;
    public readonly ref ProjectionInfo ProjectionInfo = ref projectionInfo;
}