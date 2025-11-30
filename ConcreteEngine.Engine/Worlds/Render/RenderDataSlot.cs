#region

using System.Numerics;
using ConcreteEngine.Renderer.State;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

internal static class RenderDataSlot
{
    public static RenderFrameInfo FrameInfo;
    public static ProjectionInfoData ProjectionInfo;
    public static RenderViewSnapshot ViewData;

    public static Matrix4x4 ViewMatrix => ViewData.ViewMatrix;
}