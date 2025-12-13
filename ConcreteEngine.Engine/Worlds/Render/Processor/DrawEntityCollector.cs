#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawEntityCollector
{
    public static void CollectEntities(DrawEntityContext ctx)
    {
        var view  = DrawDataProvider.WorldEntities.Core.GetCoreView();
        var len = ctx.EntitySpan.Length;
        for (var i = 0; i < len; i++)
        {
            var entityId = ctx.EntityIndices[i];
            ref var drawEntity = ref ctx.EntitySpan[i];
            ref readonly var source = ref view.GetSource(entityId);
            drawEntity.Entity = entityId;
            CollectEntity(ref drawEntity, entityId, in source);

        }
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