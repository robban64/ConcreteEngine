using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;
using Ecs = ConcreteEngine.Core.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class DrawTagResolver
{
    public static MaterialId BoundsMaterial;

    internal static void TagResolveEntities(in DrawEntityContext ctx)
    {
        var slot = 0;
        foreach (var query in Ecs.Render.Query<RenderAnimationComponent>())
        {
            var drawPtr = ctx.TryGetVisible(query.Entity);
            if (drawPtr.IsNull) continue;
            drawPtr.Value.Source.AnimatedSlot = (ushort)++slot;
        }

        if (Ecs.Render.Stores<SelectionComponent>.Store.Count == 0) return;

        foreach (var query in Ecs.Render.Query<SelectionComponent>())
        {
            var drawPtr = ctx.TryGetVisible(query.Entity);
            if (drawPtr.IsNull) continue;
            drawPtr.Value.Meta.PassMask = PassMask.Effect | PassMask.DepthPre;
            drawPtr.Value.Source.Resolver = DrawCommandResolver.Highlight;
        }
    }

    public static void UploadDebugBounds(in DrawEntityContext ctx, DrawCommandBuffer buffer)
    {
        if (Ecs.Render.Stores<DebugBoundsComponent>.Store.Count == 0) return;

        var material = BoundsMaterial;

        Span<Vector3> corners = stackalloc Vector3[8];
        foreach (var query in Ecs.Render.Query<DebugBoundsComponent>())
        {
            var entityId = query.Entity;
            var drawEntity = ctx.TryGetVisible(entityId);
            if (drawEntity.IsNull) continue;

            var depthKey = (ushort)(ushort.MaxValue - drawEntity.Value.Meta.DepthKey);
            var cmd = new DrawCommand(GfxMeshes.Cube, material, resolver: DrawCommandResolver.BoundingVolume);
            var meta = new DrawCommandMeta(DrawCommandId.Effect, DrawCommandQueue.Effect, PassMask.Effect, depthKey);
            ref var data = ref buffer.SubmitDraw(in cmd, meta);

            ref readonly var transform = ref Ecs.Render.Core.GetTransform(entityId);
            ref readonly var world = ref Ecs.Render.Core.GetParentMatrix(entityId);
            CreateBoxMatrix(corners, in Ecs.Render.Core.GetBounds(entityId), in transform, in world, out data.Model);
            data.Normal = default;
        }

        return;

        static void CreateBoxMatrix(Span<Vector3> corners, in BoundingBox local, in Transform transform,
            in Matrix4x4 world, out Matrix4x4 global)
        {
            local.FillCorners(corners);
            for (var i = 0; i < corners.Length; i++)
                corners[i] = Vector3.Transform(corners[i], world);

            BoundingAxisBox.FromPoints(corners, out var axisBounds);

            MatrixMath.CreateModelMatrix(in axisBounds.Center, in axisBounds.Extent, in transform.Rotation, out global);
        }
    }
}