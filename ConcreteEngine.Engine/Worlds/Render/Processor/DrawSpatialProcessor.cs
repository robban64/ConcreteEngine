using System.Numerics;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Utility;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawSpatialProcessor
{
    internal static void Execute(DrawEntityContext ctx)
    {
        var entities = DrawEntityStore.Entities;
        var dataEntities = DrawEntityStore.EntityData;

        if (entities.Length == 0 || dataEntities.Length == 0) return;

        var projInfo = DrawDataProvider.ProjectionInfo;

        DrawDataProvider.ViewData.ExtractView(out var viewMat);
        DepthKeyUtility.ExtractView(in viewMat, out var view);
        float near = projInfo.Near, far = projInfo.Far;

        var len = ctx.Count;

        if ((uint)len > entities.Length || (uint)len > dataEntities.Length)
            throw new IndexOutOfRangeException();

        for (var i = 0; i < len; i++)
        {
            ref var entity = ref entities[i];
            ref readonly var entityData = ref dataEntities[i];
            var depthKey = DepthKeyUtility.MakeDepthKey(in view, entityData.Transform.Translation, near, far);
            entity.WithDepthKey(depthKey);
        }
    }
}