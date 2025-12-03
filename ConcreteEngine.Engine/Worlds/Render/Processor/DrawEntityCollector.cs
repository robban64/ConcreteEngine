using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawEntityCollector
{
    internal static void CollectEntity(int idx, EntityId entityId, in RenderSourceComponent source)
    {
        ref var entity = ref DrawEntityStore.Entities[idx];
        entity = new DrawEntity(entityId, new DrawEntitySource(source.Model, source.MaterialKey, source.DrawCount));
        DrawEntityStore.ByEntityId[entityId] = idx;
    }

    internal static void CollectEntityData(int idx, in Transform transform, in BoxComponent box)
    {
        ref var entityData = ref DrawEntityStore.EntityData[idx];
        entityData.Transform = transform;
        entityData.Bounds = box.Bounds;
    }
}