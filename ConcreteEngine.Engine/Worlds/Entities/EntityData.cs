namespace ConcreteEngine.Engine.Worlds.Entities;

public readonly ref struct EntitiesCoreView(
    ReadOnlySpan<EntityId> entityId,
    ReadOnlySpan<ModelComponent> models,
    ReadOnlySpan<Transform> transforms)
{
    public ReadOnlySpan<EntityId> EntityId { get; } = entityId;
    public ReadOnlySpan<ModelComponent> Models { get; } = models;
    public ReadOnlySpan<Transform> Transforms { get; } = transforms;
    
    public int Count => EntityId.Length;
}

public ref struct EntityView(EntityId entityId, ref ModelComponent model, ref Transform transform)
{
    public readonly EntityId EntityId = entityId;
    public ref ModelComponent Model = ref model;
    public ref Transform Transform = ref transform;
}