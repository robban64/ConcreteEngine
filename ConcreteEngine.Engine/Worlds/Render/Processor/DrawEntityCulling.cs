using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.View;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawEntityCulling
{
    internal static int CullEntities(EntityId[] entityIndices, int[] byEntityId, World world)
    {
        var renderView = world.Camera.RenderView;
        var entities = world.Entities;

        var count = 0;
        BoundingBox worldBounds;
        foreach (var query in entities.CoreQuery())
        {
            RenderTransform.GetWorldBounds(in query.Box.Bounds, in query.Transform, out worldBounds);
            if (!renderView.Frustum.IntersectsBox(in worldBounds)) continue;

            byEntityId[query.Entity] = count;
            entityIndices[count++] = query.Entity;
        }

        return count;
    }
}