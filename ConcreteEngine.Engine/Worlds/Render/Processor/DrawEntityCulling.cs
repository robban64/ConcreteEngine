#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.View;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawEntityCulling
{
    internal static int CullEntities(EntityId[] entityIndices, int[] byEntityId)
    {
        var count = 0;
        BoundingBox worldBounds;
        foreach (var query in DrawDataProvider.WorldEntities.CoreQuery())
        {
            RenderTransform.GetWorldBounds(in query.Box.Bounds, in query.Transform, out worldBounds);
            if (!DrawDataProvider.Frustum.IntersectsBox(in worldBounds)) continue;

            byEntityId[query.Entity] = count;
            entityIndices[count++] = query.Entity;
        }

        return count;
    }




}