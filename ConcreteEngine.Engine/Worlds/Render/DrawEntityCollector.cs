using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Render;

internal static class DrawEntityCollector
{

    internal static void CollectEntity(int i, EntityId entityId, in RenderSourceComponent source)
    {
        ref var entity = ref DrawEntityStore.Entities[i];
        entity = new DrawEntity(entityId, new DrawEntitySource(source.Model, source.MaterialKey, source.DrawCount));
        DrawEntityStore.ByEntityId[entityId] = i;
    }

    internal static void CollectEntityData(int i, in Transform transform, in BoxComponent box)
    {
        ref var entityData = ref DrawEntityStore.EntityData[i];
        entityData.Transform = transform;
        entityData.Bounds = box.Bounds;
    }
}