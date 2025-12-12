#region

using System.Numerics;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Utility;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawSpatialProcessor
{
    internal static void TagDepthKeys(DrawEntityContext ctx)
    {
        var entities = ctx.EntitySpan;
        var dataEntities = ctx.EntityDataSpan;

        if (entities.Length == 0 || dataEntities.Length == 0) return;

        var view = DrawDataProvider.GetCameraRefView();
        var viewDepth = DepthKeyUtility.ExtractDepthVector(in view.ViewMatrix);
        var nearFar = new Vector2(view.ProjectionInfo.Near, view.ProjectionInfo.Far);

        var len = ctx.Count;
        if ((uint)len > entities.Length || (uint)len > dataEntities.Length)
            throw new IndexOutOfRangeException();

        Vector3 worldPos;
        for (var i = 0; i < len; i++)
        {
            ref var entity = ref entities[i];
            worldPos = dataEntities[i].Transform.Translation;
            var depthKey = DepthKeyUtility.MakeDepthKey(in viewDepth,  worldPos, nearFar);
            entity.WithDepthKey(depthKey);
        }
    }
}