using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Entities.Resources;

namespace ConcreteEngine.Engine.Worlds.Entities;

internal sealed class EntityCoreStore
{
    //private int _entityIdx = 0;
    private static EntityId MakeEntityId() => new(++_count);
    private static int _count = 0;

    private EntityId[] _entities;
    private SourceComponent[] _sources;
    private Transform[] _transforms;
    private BoxComponent[] _boxes;

    private Stack<int> _free = [];

    public int ActiveCount => int.Max(0, _count - 1 - _free.Count);
    public int Count => _count;

    public bool IsDirty { get; private set; }


    internal EntityCoreStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 32);
        _entities = new EntityId[initialCapacity];
        _sources = new SourceComponent[initialCapacity];
        _transforms = new Transform[initialCapacity];
        _boxes = new BoxComponent[initialCapacity];
    }

    public bool HasEntity(EntityId e)
    {
        var index = e - 1;
        return (uint)index < (uint)Count && _entities[index] == e;
    }

    public EntityId GetEntity(EntityId e) => _entities[e - 1];

    // Getters
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref SourceComponent GetSource(EntityId e) => ref _sources[e - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Transform GetTransform(EntityId e) => ref _transforms[e - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BoxComponent GetBox(EntityId e) => ref _boxes[e - 1];

    // Spans
    public Span<SourceComponent> GetSourceSpan() => _sources.AsSpan(0, _count);
    public Span<Transform> GetTransformSpan() => _transforms.AsSpan(0, _count);
    public Span<BoxComponent> GetBoxSpan() => _boxes.AsSpan(0, _count);

    // Views
    public EntityView GetEntityView(EntityId e)
    {
        var idx = e - 1;
        if ((uint)idx >= _entities.Length) throw new IndexOutOfRangeException();
        return new EntityView(e, ref _sources[idx], ref _transforms[idx], ref _boxes[idx]);
    }

    public EntitiesReadView GetReadView() =>
        new(_count, _sources.AsSpan(0, _count), _transforms.AsSpan(0, _count), _boxes.AsSpan(0, _count));

    public EntitiesCoreView GetCoreView() =>
        new(_count, _sources.AsSpan(0, _count), _transforms.AsSpan(0, _count), _boxes.AsSpan(0, _count));


    public EntityId AddEntity(in CoreComponentBundle componentBundle)
    {
        //var idx = _free.Count > 0 ? _free.Pop() : Create();
        ValidateSource(componentBundle.Source);

        var index = _count;
        var entity = MakeEntityId();
        EnsureCapacity(1);

        _entities[index] = entity;
        _sources[index] = componentBundle.Source;
        _transforms[index] = componentBundle.Transform;
        _boxes[index] = componentBundle.Box;

        IsDirty = true;
        return entity;
    }
    
    public void AddEntities(ReadOnlySpan<CoreComponentBundle> components, Span<EntityId> result)
    {
        EnsureCapacity(components.Length);
        for (var i = 0; i < components.Length; i++)
        {
            ref readonly var component = ref components[i];
            ValidateSource(component.Source);

            var index = _count;
            var entity = MakeEntityId();
            result[i] = entity;
            
            _entities[index] = entity;
            _sources[index] = component.Source;
            _transforms[index] = component.Transform;
            _boxes[index] = component.Box;
        }

        IsDirty = true;
    }

    public void Remove(EntityId e)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Value, nameof(e));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(e.Value, _count, nameof(e));
    }

    internal void EndTick()
    {
        IsDirty = false;
    }
    
    private void ValidateSource(SourceComponent source)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(source.Model.Value, nameof(source.Model));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(source.MaterialKey.Value, nameof(source.MaterialKey));
        ArgumentOutOfRangeException.ThrowIfEqual((int)source.Kind, (int)EntitySourceKind.Unknown, nameof(source.Kind));
    }

    private void EnsureCapacity(int amount)
    {
        var len = _count + amount;
        if (_entities.Length >= len) return;

        if (_sources.Length != _entities.Length || _transforms.Length != _entities.Length)
        {
            throw new InvalidOperationException();
        }

        var newSize = Arrays.CapacityGrowthSafe(_entities.Length, len);
        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _sources, newSize);
        Array.Resize(ref _transforms, newSize);
        Array.Resize(ref _boxes, newSize);

        Console.WriteLine("EntityCoreStore resize");
    }
}