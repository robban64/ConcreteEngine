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
        var entities = new UnsafeSpan<RenderEntityId>(visibleEntities);
        var indices = new UnsafeSpan<int>(visibleIndices);
        ref readonly var frustum = ref camera.GetFrustum();
        foreach (var query in Ecs.Render.CoreQuery())
        {
            BoundingBox.GetWorldBounds(in query.Box, in query.Parent, out var worldBounds);
            var visible = frustum.IntersectsBox(in worldBounds);
            visible &= query.ToggleVisibilityFlag(VisibilityFlags.Culled, visible) == 0;
            var entityIndex = query.Entity.Index();
            if (!visible)
            {
                indices[entityIndex] = -1;
                continue;
            }

            indices[entityIndex] = index;
            entities[index] = query.Entity;
            index++;
        }

        return index;
    }
    internal static void TagDepthKeys(in DrawEntityContext ctx, CameraTransform camera)
    {
        var viewDepth = DepthKeyUtility.ExtractDepthVector(in camera.ViewMatrix);
        var nearFar = new Vector2(camera.NearPlane, camera.FarPlane);
        var transformView = Ecs.Render.Core.GetTransformView();
        foreach (var it in ctx)
        {
            ref readonly var transform = ref transformView[it.Entity.Index()];
            var depthKey = DepthKeyUtility.MakeDepthKey(in viewDepth, in transform.Translation, nearFar);

            if (it.Meta.Queue >= DrawCommandQueue.Transparent)
                depthKey = (ushort)(ushort.MaxValue - depthKey);

            it.Meta.DepthKey = depthKey;
        }
    }
}