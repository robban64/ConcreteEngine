using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Utility;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class SpatialProcessor
{
    internal static int CullEntities(FrameEntityBuffer frameCtx, in CameraRenderView renderView)
    {
        var index = 0;
        BoundingBox worldBounds;
        foreach (var query in Ecs.Render.CoreQuery())
        {
            var entity = query.RenderEntity;
            ref readonly var box = ref query.Box.Bounds;
            ref readonly var parent = ref query.Parent.World;
            
            CameraUtils.GetWorldBounds(in box, in parent, out worldBounds);
            if (!renderView.Frustum.IntersectsBox(in worldBounds)) continue;
            frameCtx.IncrementVisible(entity, index);
            index++;
        }

        return index;
    }

    internal static void TagDepthKeys(in DrawEntityContext ctx, in CameraRenderView renderView)
    {
        var viewDepth = DepthKeyUtility.ExtractDepthVector(in renderView.ViewMatrix);
        var nearFar = new Vector2(renderView.ProjectionInfo.Near, renderView.ProjectionInfo.Far);

        var transformSpan = new UnsafeSpan<RenderTransform>(Ecs.Render.Core.GetTransformSpan());
        foreach (var it in ctx)
        {
            ref var entity = ref it.DrawEntity;
            var translation = transformSpan[entity.RenderEntity.Index()].Transform.Translation;
            var depthKey = DepthKeyUtility.MakeDepthKey(in viewDepth, in translation, nearFar);

            if (entity.Meta.Queue >= DrawCommandQueue.Transparent)
                depthKey = (ushort)(ushort.MaxValue - depthKey);
                    
            entity.Meta.DepthKey = depthKey;
        }
    }


}