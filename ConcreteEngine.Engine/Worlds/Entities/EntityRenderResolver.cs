using System.Runtime.InteropServices;
using ConcreteEngine.Common;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Entities;

internal readonly struct EntityResolverEntry : IComparable<EntityResolverEntry>
{
    public readonly EntityHandle Entity;
    public readonly DrawCommandResolver CommandResolver;

    public EntityResolverEntry(EntityHandle entity, RenderResolver resolver)
    {
        Entity = entity;

        CommandResolver = resolver switch
        {
            RenderResolver.Wireframe => DrawCommandResolver.Wireframe,
            RenderResolver.Highlight => DrawCommandResolver.Highlight,
            RenderResolver.BoundingVolume => DrawCommandResolver.BoundingVolume,
            _ => throw new ArgumentOutOfRangeException(nameof(resolver), resolver, null)
        };
    }

    public int CompareTo(EntityResolverEntry other) => Entity.Value == 0 ? -1 : Entity.Value.CompareTo(other.Entity.Value);
}

internal sealed class EntityRenderResolver
{
    private readonly List<EntityResolverEntry> _resolvedEntities = new(16);

    internal ReadOnlySpan<EntityResolverEntry> Entities => CollectionsMarshal.AsSpan(_resolvedEntities);

    public void AddResolver(EntityHandle entity, RenderResolver resolver)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Value, nameof(entity));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero((int)resolver, nameof(resolver));

        _resolvedEntities.Add(new EntityResolverEntry(entity, resolver));
    }

    public void RemoveResolver(EntityHandle entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Value, nameof(entity));

        var entities = Entities;

        int foundIndex = -1;
        for (int i = 0; i < entities.Length; i++)
        {
            var entry = entities[i];
            if (entry.Entity != entity) continue;
            foundIndex = i;
            break;
        }

        InvalidOpThrower.ThrowIf(foundIndex == -1, "Entity not found");
        _resolvedEntities.RemoveAt(foundIndex);
    }
}