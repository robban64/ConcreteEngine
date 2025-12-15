using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawEntityCollector
{
    public static EntityId CollectEntities(in DrawEntityContext ctx, in EntitiesReadView view)
    {
        var len = ctx.EntitySpan.Length;
        int highEntityId = 0;

        for (var i = 0; i < len; i++)
        {
            var entityId = ctx.EntityIndices[i];
            ref var drawEntity = ref ctx.EntitySpan[i];
            ref readonly var source = ref view.GetSource(entityId);
            
            drawEntity.Entity = entityId;
            drawEntity.Source = new DrawEntitySource(source.Model, source.MaterialKey, source.DrawCount);
            drawEntity.Meta = new DrawEntityMeta(DrawCommandId.Model, DrawCommandQueue.Opaque, PassMask.Default);
            
            highEntityId = int.Max(highEntityId, entityId);
        }

        return new EntityId(highEntityId);
    }

}