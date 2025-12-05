using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawEntityCollector
{
    
    internal static void CollectEntity(int idx,DrawEntityContext ctx, EntityId entityId, in RenderSourceComponent source)
    {
        ref var entity = ref ctx.EntitySpan[idx];
        entity = new DrawEntity(entityId, new DrawEntitySource(source.Model, source.MaterialKey, source.DrawCount));
        if (source.Kind == RenderSourceKind.Particle)
            entity.Meta = new DrawEntityMeta(DrawCommandId.Particle, DrawCommandQueue.Particles, PassMask.Main);
        
        ctx.ByEntityIdSpan[entityId] = idx;
    }

}