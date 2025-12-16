using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Worlds.Entities.Components;

namespace ConcreteEngine.Engine.Worlds.Entities;

internal readonly ref struct EntitiesCoreView(
    int count,
    Span<SourceComponent> sources,
    Span<Transform> transforms,
    Span<BoxComponent> boxes)
{
    public readonly Span<SourceComponent> Sources = sources;
    public readonly Span<Transform> Transforms = transforms;
    public readonly Span<BoxComponent> Boxes = boxes;

    public readonly int Count = count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly SourceComponent GetSource(EntityId entity) => ref Sources[entity - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly Transform GetTransform(EntityId entity) => ref Transforms[entity - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly BoxComponent GetBox(EntityId entity) => ref Boxes[entity - 1];
}

internal readonly ref struct EntitiesReadView(
    int count,
    ReadOnlySpan<SourceComponent> sources,
    ReadOnlySpan<Transform> transforms,
    ReadOnlySpan<BoxComponent> boxes)
{
    public readonly ReadOnlySpan<SourceComponent> Sources = sources;
    public readonly ReadOnlySpan<Transform> Transforms = transforms;
    public readonly ReadOnlySpan<BoxComponent> Boxes = boxes;

    public readonly int Count = count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly SourceComponent GetSource(EntityId entity) => ref Sources[entity - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly Transform GetTransform(EntityId entity) => ref Transforms[entity - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly BoxComponent GetBox(EntityId entity) => ref Boxes[entity - 1];
}

