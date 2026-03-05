using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class RenderEntityCollector
{
    public static void CollectEntities(in DrawEntityContext ctx)
    {
        var zip = new UnsafeZippedSpan<RenderEntityId, DrawEntity>(ctx.EntityIndices, ctx.EntitySpan);
        var len = zip.Length;
        for (var i = 0; i < len; i++)
        {
            var entityPtr = zip[i];
            var entityId = entityPtr.Item1;
            var sourcePtr = Ecs.Render.Core.TryGetSource(entityId);
            if (sourcePtr.IsNull) continue;

            ref readonly var source = ref sourcePtr.Value;
            ref var drawEntity = ref entityPtr.Item2;

            drawEntity.RenderEntity = entityId;
            drawEntity.Source = new DrawEntitySource(source.Mesh, source.Material);
            drawEntity.Meta = new DrawEntityMeta(DrawCommandId.Model, source.Queue, source.Mask);
        }
    }

    public static void UploadDrawCommands(DrawCommandBuffer buffer, in DrawEntityContext ctx)
    {
        var ecsRenderCore = Ecs.Render.Core;

        foreach (ref var it in ctx)
        {
            var cmd = new DrawCommand(it.Source.Mesh, it.Source.Material, it.Source.InstanceCount,
                it.Source.AnimatedSlot, it.Source.Resolver);

            ref readonly var world = ref ecsRenderCore.GetParentMatrix(it.RenderEntity);
            ref var data = ref buffer.SubmitDraw(in cmd, Unsafe.As<DrawEntityMeta, DrawCommandMeta>(ref it.Meta));
            data.Model = world;
            MatrixMath.CreateNormalMatrix(in world, out data.Normal);
        }
    }
}