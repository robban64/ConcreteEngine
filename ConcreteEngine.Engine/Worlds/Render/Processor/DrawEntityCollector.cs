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