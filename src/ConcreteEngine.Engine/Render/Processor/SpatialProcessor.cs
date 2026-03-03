using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Utility;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class SpatialProcessor
{
    internal static int CullEntities(FrameEntityBuffer frameCtx, Camera camera)
    {
        var index = 0;
        BoundingBox worldBounds;
        foreach (var query in Ecs.Render.CoreQuery())
        {
            CameraUtils.GetWorldBounds(in query.Box, in query.Parent, out worldBounds);
            if (!camera.GetFrustum().IntersectsBox(in worldBounds)) continue;
            frameCtx.IncrementVisible(query.RenderEntity, index);
            index++;
        }

        return index;
    }

    internal static void TagDepthKeys(in DrawEntityContext ctx, Camera camera)
    {
        var viewDepth = DepthKeyUtility.ExtractDepthVector(in camera.GetViewMatrix());
        var nearFar = new Vector2(camera.NearPlane, camera.FarPlane);

        var transformSpan = new UnsafeSpan<Transform>(Ecs.Render.Core.GetTransformSpan());
        foreach (ref var entity in ctx)
        {
            var translation = transformSpan[entity.RenderEntity.Index()].Translation;
            var depthKey = DepthKeyUtility.MakeDepthKey(in viewDepth, in translation, nearFar);

            if (entity.Meta.Queue >= DrawCommandQueue.Transparent)
                depthKey = (ushort)(ushort.MaxValue - depthKey);

            entity.Meta.DepthKey = depthKey;
        }
    }
}