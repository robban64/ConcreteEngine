using ConcreteEngine.Common;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Entities;

internal readonly struct EntityResolverEntry : IComparable<EntityResolverEntry>
{
    public readonly EntityId Entity;
    public readonly RenderResolver Resolver;
    public readonly DrawCommandResolver CommandResolver;
    public readonly bool IsAnimated;

    public EntityResolverEntry(EntityId entity, RenderResolver resolver, bool isAnimated)
    {
        Entity = entity;
        Resolver = resolver;
        IsAnimated = isAnimated;

        CommandResolver = resolver switch
        {
            RenderResolver.Wireframe => DrawCommandResolver.Wireframe,
            RenderResolver.Highlight => isAnimated
                ? DrawCommandResolver.HighlightAnimated
                : DrawCommandResolver.Highlight,
            RenderResolver.BoundingVolume => DrawCommandResolver.BoundingVolume,
            _ => throw new ArgumentOutOfRangeException(nameof(resolver), resolver, null)
        };
    }

    public int CompareTo(EntityResolverEntry other) => Entity.Id == 0 ? -1 : Entity.Id.CompareTo(other.Entity.Id);
}

internal sealed class EntityRenderResolver
{
    private readonly EntityResolverEntry[] _resolvedEntities = new EntityResolverEntry[16];
    //private readonly Stack<int> _free = new(4);

    private int _idx;

    public ReadOnlySpan<EntityResolverEntry> Entities => _resolvedEntities.AsSpan(0, _idx);

    public void AddResolver(EntityId entity, RenderResolver resolver, bool isAnimated)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero((int)resolver, nameof(resolver));

        //var index = _free.Count > 0 ? _free.Pop() : _idx++;
        _resolvedEntities[_idx++] = new EntityResolverEntry(entity, resolver, isAnimated);
        if (_idx > 1)
            _resolvedEntities.AsSpan(0, _idx).Sort();
    }

    public void RemoveResolver(EntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));

        //var index = _free.Count > 0 ? _free.Pop() : _idx++;
        int foundIndex = -1;
        for (int i = 0; i < _idx; i++)
        {
            var entry = _resolvedEntities[i];
            if (entry.Entity != entity) continue;
            foundIndex = i;
            break;
        }

        InvalidOpThrower.ThrowIf(foundIndex == -1, "Entity not found");

        _resolvedEntities[foundIndex] = default;
        if (_idx > 1)
            _resolvedEntities.AsSpan(0, _idx).Sort();

        _idx--;
    }
}