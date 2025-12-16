using ConcreteEngine.Engine.Worlds.Entities.Components;

namespace ConcreteEngine.Engine.Worlds.Entities.Resources;

internal readonly ref struct EntityView(
    EntityId entityId,
    ref SourceComponent source,
    ref Transform transform,
    ref BoxComponent box)
{
    public readonly EntityId EntityId = entityId;
    public readonly ref SourceComponent Source = ref source;
    public readonly ref Transform Transform = ref transform;
    public readonly ref BoxComponent Box = ref box;
}