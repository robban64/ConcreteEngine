using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Worlds.Utility;
using Ecs = ConcreteEngine.Core.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class SpatialProcessor
{
    internal static int CullEntities(Span<RenderEntityId> visibleEntities, Span<int> visibleIndices, CameraTransform camera)
    {
        var index = 0;
        BoundingBox worldBounds;
        foreach (var query in Ecs.Render.CoreQuery())
        {
            BoundingBox.GetWorldBounds(in query.Box, in query.Parent, out worldBounds);
            var visible = camera.GetFrustum().IntersectsBox(in worldBounds);
            visible &= query.ToggleVisibilityFlag(VisibilityFlags.Culled, visible) == 0;
            if (!visible)
            {
                visibleIndices[query.RenderEntity.Index()] = -1;
                continue;
            }

            visibleIndices[query.RenderEntity.Index()] = index;
            visibleEntities[index] = query.RenderEntity;
            index++;
        }

        return index;
    }

    internal static void TagDepthKeys(in DrawEntityContext ctx, CameraTransform camera)
    {
        var viewDepth = DepthKeyUtility.ExtractDepthVector(in camera.ViewMatrix);
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