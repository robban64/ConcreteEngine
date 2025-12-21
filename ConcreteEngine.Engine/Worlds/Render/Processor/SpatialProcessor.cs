using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Utility;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class SpatialProcessor
{
    internal static int CullEntities(RenderEntityId[] entityIndices, int[] byEntityId, RenderEntityHub renderEntityHub,
        in CameraRenderView renderView)
    {
        var count = 0;
        BoundingBox worldBounds;
        foreach (var query in renderEntityHub.CoreQuery())
        {
            CameraUtils.GetWorldBounds(in query.Box.Bounds, in query.Transform.Transform, out worldBounds);
            if (!renderView.Frustum.IntersectsBox(in worldBounds)) continue;
            var entity = query.RenderEntity;
            byEntityId[entity] = count;
            entityIndices[count++] = entity;
        }

        return count;
    }

    internal static void TagDepthKeys(in DrawEntityContext ctx, in CameraRenderView renderView, RenderEntityCore ecs)
    {
        var viewDepth = DepthKeyUtility.ExtractDepthVector(in renderView.ViewMatrix);
        var nearFar = new Vector2(renderView.ProjectionInfo.Near, renderView.ProjectionInfo.Far);

        var transformSpan = ecs.GetTransformSpan();
        foreach (var it in ctx)
        {
            ref var entity = ref it.DrawEntity;
            var index = entity.RenderEntity.Index();
            ref readonly var t = ref transformSpan[index].Transform;
            var depthKey = DepthKeyUtility.MakeDepthKey(in viewDepth, t.Translation, nearFar);
            entity.Meta.DepthKey = depthKey;
        }
    }
}