using ConcreteEngine.Engine.Worlds.Entities.Components;

namespace ConcreteEngine.Engine.Worlds.Entities;

internal readonly ref struct EntitiesCoreView(
    ReadOnlySpan<EntityId> entityId,
    ReadOnlySpan<RenderSourceComponent> sources,
    ReadOnlySpan<Transform> transforms,
    ReadOnlySpan<BoxComponent> boxes)
{
    public readonly ReadOnlySpan<EntityId> EntityId = entityId;
    public readonly ReadOnlySpan<RenderSourceComponent> Sources = sources;
    public readonly ReadOnlySpan<Transform> Transforms = transforms;
    public readonly ReadOnlySpan<BoxComponent> Boxes = boxes;

    public ref readonly Transform GetTransform(int index) => ref Transforms[index];
    public ref readonly BoxComponent GetBox(int index) => ref Boxes[index];

    public int Count => EntityId.Length;
}

internal readonly ref struct EntityView(
    EntityId entityId,
    ref RenderSourceComponent source,
    ref Transform transform,
    ref BoxComponent box)
{
    public readonly EntityId EntityId = entityId;
    public readonly ref RenderSourceComponent Source = ref source;
    public readonly ref Transform Transform = ref transform;
    public readonly ref BoxComponent Box = ref box;
}

internal ref struct EntityCoreWriter(
    EntityId entityId,
    ref RenderSourceComponent source,
    ref Transform transform,
    ref BoxComponent box)
{
    public readonly EntityId EntityId = entityId;
    public ref RenderSourceComponent Source = ref source;
    public ref Transform Transform = ref transform;
    public ref BoxComponent Box = ref box;
}