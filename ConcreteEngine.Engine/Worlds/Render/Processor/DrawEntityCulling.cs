using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Engine.Worlds.View;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawEntityCulling
{
    internal static int CullEntities(EntityId[] entityIndices, int[] byEntityId, WorldEntities worldEntities, Camera3D camera)
    {
        var renderView = camera.RenderView;
        
        var count = 0;
        BoundingBox worldBounds;
        foreach (var query in worldEntities.CoreQuery())
        {
            RenderTransform.GetWorldBounds(in query.Box.Bounds, in query.Transform, out worldBounds);
            if (!renderView.Frustum.IntersectsBox(in worldBounds)) continue;
            var entity = query.Entity;
            byEntityId[entity] = count;
            entityIndices[count++] = entity;
        }

        return count;
    }
    
    internal static void TagDepthKeys(in DrawEntityContext ctx, in EntitiesReadView view,
        in CameraRenderView renderView)
    {
        var viewDepth = DepthKeyUtility.ExtractDepthVector(in renderView.ViewMatrix);
        var nearFar = new Vector2(renderView.ProjectionInfo.Near, renderView.ProjectionInfo.Far);

        foreach (var it in ctx)
        {
            ref var entity = ref it.DrawEntity;
            ref readonly var transform = ref view.GetTransform(entity.Entity);
            var depthKey = DepthKeyUtility.MakeDepthKey(in viewDepth, in transform.Translation, nearFar);
            entity.Meta.DepthKey = depthKey;
        }
    }

}