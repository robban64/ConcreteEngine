using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Worlds.Entities.Components;

namespace ConcreteEngine.Engine.Worlds.Entities;

internal readonly ref struct EntitiesCoreView(
    Span<RenderSourceComponent> sources,
    Span<Transform> transforms,
    Span<BoxComponent> boxes)
{
    public readonly Span<RenderSourceComponent> Sources = sources;
    public readonly Span<Transform> Transforms = transforms;
    public readonly Span<BoxComponent> Boxes = boxes;

    public int Count => Sources.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly RenderSourceComponent GetSource(EntityId entity) => ref Sources[entity - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly Transform GetTransform(EntityId entity) => ref Transforms[entity - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly BoxComponent GetBox(EntityId entity) => ref Boxes[entity - 1];
}


internal readonly ref struct EntitiesReadView(
    ReadOnlySpan<RenderSourceComponent> sources,
    ReadOnlySpan<Transform> transforms,
    ReadOnlySpan<BoxComponent> boxes)
{
    public readonly ReadOnlySpan<RenderSourceComponent> Sources = sources;
    public readonly ReadOnlySpan<Transform> Transforms = transforms;
    public readonly ReadOnlySpan<BoxComponent> Boxes = boxes;

    public int Count => Sources.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly RenderSourceComponent GetSource(EntityId entity) => ref Sources[entity - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly Transform GetTransform(EntityId entity) => ref Transforms[entity - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly BoxComponent GetBox(EntityId entity) => ref Boxes[entity - 1];
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