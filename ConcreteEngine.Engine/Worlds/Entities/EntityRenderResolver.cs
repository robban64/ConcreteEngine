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
    private EntityResolverEntry[] _resolvedEntities = new EntityResolverEntry[16];
    private readonly Stack<int> _free = new(4);

    private int _idx;

    public ReadOnlySpan<EntityResolverEntry> Entities => _resolvedEntities.AsSpan(0, _idx);

    public void AddResolver(EntityId entity, RenderResolver resolver)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero((int)resolver, nameof(resolver));

        var index = _free.Count > 0 ? _free.Pop() : _idx++;
        _resolvedEntities[index] = new EntityResolverEntry(entity, resolver);
    }

    public void RemoveResolver(EntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));

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
        _free.Push(foundIndex);
    }
    
    private int Allocate(int n)
    {
        var len = _resolvedEntities.Length;
        var count = _idx + n;
        if (_idx >= len)
        {
            var newCap = Arrays.CapacityGrowthSafe(len, count);
            Console.WriteLine("Entity Resolver store resize");

            if (newCap > GfxLimits.StoreLimit)
                throw new InvalidOperationException("Store limit exceeded");

            Array.Resize(ref _resolvedEntities, newCap);
        }

        return _idx++;
    }

}