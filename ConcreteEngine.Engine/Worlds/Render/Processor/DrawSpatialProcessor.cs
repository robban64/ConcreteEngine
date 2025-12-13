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
        var view = DrawDataProvider.GetCameraRefView();
        var viewDepth = DepthKeyUtility.ExtractDepthVector(in view.ViewMatrix);
        var nearFar = new Vector2(view.ProjectionInfo.Near, view.ProjectionInfo.Far);

        var transformSpan = DrawDataProvider.WorldEntities.Core.GetTransformSpan();
        var entities = ctx.EntitySpan;
        var len = entities.Length;
        if ((uint)len > transformSpan.Length)
            throw new IndexOutOfRangeException();

        Vector3 worldPos;
        for (var i = 0; i < len; i++)
        {
            ref var entity = ref entities[i];
            worldPos = transformSpan[i].Translation;
            var depthKey = DepthKeyUtility.MakeDepthKey(in viewDepth, worldPos, nearFar);
            entity.WithDepthKey(depthKey);
        }
    }
}