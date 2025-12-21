using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawEntityCollector
{
    public static RenderEntityId CollectEntities(in DrawEntityContext ctx, RenderEntityCore coreEcs)
    {
        var len = ctx.EntitySpan.Length;
        var highEntityId = 0;

        var ecsSourceSpan = coreEcs.GetSourceSpan();
        for (var i = 0; i < len; i++)
        {
            var entityId = ctx.EntityIndices[i];
            ref var drawEntity = ref ctx.EntitySpan[i];
            ref readonly var source = ref ecsSourceSpan[entityId.Index()];
            
            drawEntity.RenderEntity = entityId;
            drawEntity.Source = new DrawEntitySource(source.Model, source.MaterialKey);
            drawEntity.Meta = new DrawEntityMeta(DrawCommandId.Model, DrawCommandQueue.Opaque, PassMask.Default);
            
            highEntityId = int.Max(highEntityId, entityId);
        }

        return new RenderEntityId(highEntityId);
    }
}