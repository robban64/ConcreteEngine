using System.Numerics;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;
using Ecs = ConcreteEngine.Core.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class DrawTagProcessor
{
    public static MaterialId BoundsMaterial;

    public static void TagUploadSelectionEffect(in DrawEntityContext ctx, EffectBuffer effects)
    {
        if (Ecs.Render.Stores<SelectionComponent>.Store.Count == 0) return;

        foreach (var query in Ecs.Render.Query<SelectionComponent>())
        {
            var drawItem = ctx.TryGetVisible(query.Entity);
            if (drawItem.Entity.Id == 0) continue;

            var slot = effects.Submit(new EffectUniformParams(query.Component.HighlightColor));
            drawItem.Meta.Resolver = DrawCommandResolver.Highlight;
            drawItem.Meta.PassMask = PassMask.Effect | PassMask.Depth;
            drawItem.Meta.ResolverSlot = slot;
        }
    }

    public static void UploadDebugBounds(int submitOffset, Span<int> visibleIndices, DrawCommandBuffer buffer,
        EffectBuffer effects)
    {
        if (Ecs.Render.Stores<DebugBoundsComponent>.Store.Count == 0) return;

        var ecs = Ecs.Render.Core;
        var material = BoundsMaterial;

        var drawCommands = buffer.GetDrawCommands(0);
        var indices = new UnsafeSpan<int>(visibleIndices);

        Span<Vector3> corners = stackalloc Vector3[8];
        foreach (var query in Ecs.Render.Query<DebugBoundsComponent>())
        {
            var entity = query.Entity;
            var index = indices[entity.Index()];
            if (index < 0) continue;

            var depthKey = (ushort)(ushort.MaxValue - drawCommands.At2(submitOffset + index).DepthKey);
            var slot = effects.Submit(new EffectUniformParams(query.Component.Color));

            drawCommands.At1(buffer.Count) = new DrawCommand(GfxMeshes.Cube, material);

            drawCommands.At2(buffer.Count) =
                new DrawCommandMeta(DrawCommandId.Effect, DrawCommandQueue.Effect, PassMask.Effect, depthKey,
                    DrawCommandResolver.BoundingVolume, resolverSlot: slot);

            ref var data = ref buffer.SubmitDraw();

            ref readonly var transform = ref ecs.GetTransform(entity);
            ref readonly var world = ref ecs.GetMatrix(entity);
            CreateBoxMatrix(corners, in ecs.GetBounds(entity), in transform, in world, out data.Model);
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