using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.ECS;
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

    public static void UploadDrawCommands(in DrawCommandUploader uploader, in DrawEntityContext ctx)
    {
        var ecsRenderCore = Ecs.Render.Core;

        foreach (var it in ctx)
        {
            ref var entity = ref it.DrawEntity;
            ref readonly var source = ref it.DrawEntity.Source;
            ref readonly var world = ref ecsRenderCore.GetParentMatrix(entity.RenderEntity).World;

            var cmd = new DrawCommand(source.Mesh, source.Material, source.InstanceCount,
                source.AnimatedSlot, source.Resolver);
            
            var data = uploader.SubmitDraw(in cmd, Unsafe.As<DrawEntityMeta, DrawCommandMeta>(ref entity.Meta));
            data.Value.Model = world;
            MatrixMath.CreateNormalMatrix(in world, out data.Value.Normal);
        }
    }
}