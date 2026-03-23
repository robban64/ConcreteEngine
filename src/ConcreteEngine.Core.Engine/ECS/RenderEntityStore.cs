using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.ECS.Integration;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.ECS;

public interface IRenderEntityStore;

public sealed class RenderEntityStore<T> : EcsStore, IRenderEntityStore where T : unmanaged, IRenderComponent<T>
{
    private T[] _data;
    private RenderEntityId[] _entities;

    private readonly List<IRenderComponentListener<T>> _listeners = new(32);

    public RenderEntityStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 16);
        _data = new T[initialCapacity];
        _entities = new RenderEntityId[initialCapacity];
    }

    public override int Capacity => _entities.Length;
    public override EcsStoreType StoreType => EcsStoreType.Render;

    internal override void Initialize()
    {
        InvalidOpThrower.ThrowIf(_entities.Length == 0, nameof(_entities));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(RenderEntityId renderEntity) => FindIndex(renderEntity) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderEntityId GetEntity(int i) => _entities[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetByIndex(int i) => ref _data[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(RenderEntityId entity) => ref _data[FindIndex(entity)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOrDefault(RenderEntityId entity)
    {
        var id = FindIndex(entity);
        if ((uint)id >= _data.Length) return default;
        return _data[id];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtr<T> TryGet(RenderEntityId entity)
    {
        var id = FindIndex(entity);
        if ((uint)id >= _data.Length) return ValuePtr<T>.Null;
        return new ValuePtr<T>(ref _data[id]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<RenderEntityId> GetEntitySpan() => _entities.AsSpan(0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetComponentSpan() => _data.AsSpan(0, Count);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndex(RenderEntityId entity) => SortMethod.BinarySearch(GetEntitySpan(), entity);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Add(RenderEntityId entity, in T value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        var index = AllocateNext();

        _entities[index] = entity;
        _data[index] = value;

        ref var data = ref _data[index];
        foreach (var it in _listeners)
            it.ComponentAdded(entity, ref data);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Remove(RenderEntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));

        var idx = FindIndex(entity);
        if (idx == -1) throw new ArgumentOutOfRangeException(nameof(entity));

        ref var data = ref _data[idx];
        foreach (var it in _listeners)
            it.ComponentRemoved(entity, ref data);

        _entities[idx] = default;
        data = default;
        FreeEntity(idx);
    }

    public void BindListener(IRenderComponentListener<T> listener) => _listeners.Add(listener);
    public void UnbindListener(IRenderComponentListener<T> listener) => _listeners.Remove(listener);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ecs.RenderQuery<T>.RenderEntityEnumerator Query() => new(this);


    protected override void Resize(int newSize)
    {
        if (_data.Length != _entities.Length)
            throw new InvalidOperationException();

        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _data, newSize);
    }
}