using ConcreteEngine.Engine.Worlds.Entities.Components;

namespace ConcreteEngine.Engine.Worlds.Entities;

public readonly ref struct EntitiesCoreView(
    ReadOnlySpan<EntityId> entityId,
    ReadOnlySpan<RenderSourceComponent> sources,
    ReadOnlySpan<Transform> transforms)
{
    public ReadOnlySpan<EntityId> EntityId { get; } = entityId;
    public ReadOnlySpan<RenderSourceComponent> Sources { get; } = sources;
    public ReadOnlySpan<Transform> Transforms { get; } = transforms;
    
    public int Count => EntityId.Length;
}

public ref struct EntityView(EntityId entityId, ref RenderSourceComponent model, ref Transform transform)
{
    public readonly EntityId EntityId = entityId;
    public ref RenderSourceComponent Source = ref model;
    public ref Transform Transform = ref transform;
}