using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
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
            var drawItem = ctx.TryGetVisible(query.Entity);
            if (drawItem.Entity == 0) continue;
            drawItem.Command.AnimationSlot = (ushort)++slot;
        }

        if (Ecs.Render.Stores<SelectionComponent>.Store.Count == 0) return;

        foreach (var query in Ecs.Render.Query<SelectionComponent>())
        {
            var drawItem = ctx.TryGetVisible(query.Entity);
            if (drawItem.Entity == 0) continue;
            drawItem.Command.Resolver = DrawCommandResolver.Highlight;
            drawItem.Meta.PassMask = PassMask.Effect | PassMask.DepthPre;
        }
    }

    public static void UploadDebugBounds(int submitOffset, Span<int> visibleIndices, DrawCommandBuffer buffer)
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

            drawCommands.At1(buffer.Count) =
                new DrawCommand(GfxMeshes.Cube, material, resolver: DrawCommandResolver.BoundingVolume);
            
            drawCommands.At2(buffer.Count) = 
                new DrawCommandMeta(DrawCommandId.Effect, DrawCommandQueue.Effect, PassMask.Effect, depthKey);
            
            ref var data = ref buffer.SubmitDraw();

            ref readonly var transform = ref ecs.GetTransform(entity);
            ref readonly var world = ref ecs.GetParentMatrix(entity);
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