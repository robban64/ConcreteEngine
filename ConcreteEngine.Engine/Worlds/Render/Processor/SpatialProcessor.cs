using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Utility;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class SpatialProcessor
{
    internal static int CullEntities(RenderEntityId[] entityIndices, int[] byEntityId, in CameraRenderView renderView)
    {
        var count = 0;
        BoundingBox worldBounds;
        foreach (var query in Ecs.Render.CoreQuery())
        {
            CameraUtils.GetWorldBounds(in query.Box.Bounds, in query.Transform.Transform, out worldBounds);
            if (!renderView.Frustum.IntersectsBox(in worldBounds)) continue;
            var entity = query.RenderEntity;
            byEntityId[entity] = count;
            entityIndices[count++] = entity;
        }

        return count;
    }

    internal static void TagDepthKeys(in DrawEntityContext ctx, Camera camera)
    {
        var renderView = camera.RenderView;
        var viewDepth = DepthKeyUtility.ExtractDepthVector(in renderView.ViewMatrix);
        var nearFar = new Vector2(renderView.ProjectionInfo.Near, renderView.ProjectionInfo.Far);

        foreach (var it in ctx)
        {
            ref var entity = ref it.DrawEntity;
            var tPtr =  Ecs.Render.Core.TryGetTransform(entity.RenderEntity);
            if(tPtr.IsNull) continue;
            var depthKey = DepthKeyUtility.MakeDepthKey(in viewDepth, tPtr.Value.Transform.Translation, nearFar);
            entity.Meta.DepthKey = depthKey;
        }
    }
}