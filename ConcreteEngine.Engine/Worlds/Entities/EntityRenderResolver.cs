using System.Runtime.InteropServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Graphics;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Entities;

internal readonly struct EntityResolverEntry : IComparable<EntityResolverEntry>
{
    public readonly EntityId Entity;
    public readonly DrawCommandResolver CommandResolver;

    public EntityResolverEntry(EntityId entity, RenderResolver resolver)
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

    public int CompareTo(EntityResolverEntry other) => Entity.Id == 0 ? -1 : Entity.Id.CompareTo(other.Entity.Id);
}

internal sealed class EntityRenderResolver
{
    private readonly List<EntityResolverEntry> _resolvedEntities = new (16);

    internal ReadOnlySpan<EntityResolverEntry> Entities => CollectionsMarshal.AsSpan(_resolvedEntities);

    public void AddResolver(EntityId entity, RenderResolver resolver)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero((int)resolver, nameof(resolver));

        _resolvedEntities.Add(new EntityResolverEntry(entity, resolver));
    }

    public void RemoveResolver(EntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));

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