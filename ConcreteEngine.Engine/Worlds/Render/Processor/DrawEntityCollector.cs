using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawEntityCollector
{
    public static EntityId CollectEntities(DrawEntityContext ctx)
    {
        var view = DrawDataProvider.WorldEntities.Core.GetCoreView();
        var len = ctx.EntitySpan.Length;
        int _highEntityId = 0;

        for (var i = 0; i < len; i++)
        {
            var entityId = ctx.EntityIndices[i];
            ref var drawEntity = ref ctx.EntitySpan[i];
            ref readonly var source = ref view.GetSource(entityId);
            drawEntity.Entity = entityId;
            _highEntityId = int.Max(_highEntityId, entityId);
            CollectEntity(ref drawEntity, entityId, in source);
        }

        return new EntityId(_highEntityId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CollectEntity(ref DrawEntity entity, EntityId entityId, in RenderSourceComponent source)
    {
        entity.Entity = entityId;
        entity.Source = new DrawEntitySource(source.Model, source.MaterialKey, source.DrawCount);
        entity.Meta = new DrawEntityMeta(DrawCommandId.Model, DrawCommandQueue.Opaque, PassMask.Default);
        if (source.Kind == RenderSourceKind.Particle)
            entity.Meta = new DrawEntityMeta(DrawCommandId.Particle, DrawCommandQueue.Particles, PassMask.Main);
    }
}