using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public interface IRenderEntityStore : IDisposable;

public sealed unsafe partial class RenderEntityStore<T> : IRenderEntityStore where T : unmanaged, IRenderComponent<T>
{
    public static RenderEntityStore<T> Instance { get; internal set; } = null!;
    
    public bool IsDirty { get; private set; }
    public int Count { get; private set; }
    public int Capacity { get; private set; }

    private T* _components;
    private RenderEntityId* _entities;

    private readonly List<RenderEntityId> _removedEntities = [];
    private readonly List<IRenderComponentListener<T>> _listeners = [];

    public RenderEntityStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 16);
        _components = NativeArray.AllocatePointer<T>(initialCapacity);
        _entities = NativeArray.AllocatePointer<RenderEntityId>(initialCapacity);
        Capacity = initialCapacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndexSorted(RenderEntityId entity) =>
        SearchMethod.BinarySearch(new ReadOnlySpan<RenderEntityId>(_entities, Count), entity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndexLinear(RenderEntityId entity)
    {
        var view = EntitiesView().Reinterpret<int>();
        return view.AsReadOnlySpan().IndexOf(entity.Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(RenderEntityId entity) => FindIndexLinear(entity) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderEntityId GetEntity(int i) => _entities[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetByIndex(int i) => ref _components[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(RenderEntityId entity) => ref _components[FindIndexSorted(entity)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOrDefault(RenderEntityId entity)
    {
        var index = FindIndexSorted(entity);
        if ((uint)index >= (uint)Count) return default;
        return _components[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(RenderEntityId entity, out ValueRef<T> value)
    {
        var index = FindIndexLinear(entity);
        if ((uint)index < (uint)Count)
        {
            value = new ValueRef<T>(ref _components[index]);
            return true;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<RenderEntityId> EntitiesView() => new(_entities, 0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<T> ComponentsView() => new(_components, 0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<RenderEntityId> EntitySpan() => new(_entities, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> ComponentSpan() => new(_components, Count);

    public bool Add(RenderEntityId entity, in T value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        if (Has(entity)) return false;
        if (Count >= Capacity) EnsureCapacity(1);
        
        var index = Count++;
        _entities[index] = entity;
        _components[index] = value;

        if (_listeners.Count > 0)
        {
            foreach (var listener in CollectionsMarshal.AsSpan(_listeners))
                listener.ComponentAdded(entity, ref _components[index]);
        }

        IsDirty = true;
        return true;
    }

    public bool Remove(RenderEntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        if (!Has(entity)) return false;
        _removedEntities.Add(entity);
        IsDirty = true;
        return true;
    }

    public void Commit()
    {
        if (!IsDirty) return;
        IsDirty = false;

        if (_removedEntities.Count > 0) CommitRemoved();

        EntitySpan().Sort(ComponentSpan());
        return;

        void CommitRemoved()
        {
            foreach (var entity in CollectionsMarshal.AsSpan(_removedEntities))
            {
                var index = FindIndexLinear(entity);
                if (_listeners.Count > 0)
                {
                    foreach (var listener in CollectionsMarshal.AsSpan(_listeners))
                        listener.ComponentRemoved(entity, ref _components[index]);
                }

                var count = --Count;
                _entities[index] = _entities[count];
                _components[index] = _components[count];

                _entities[count] = default;
                _components[count] = default;
            }

            _removedEntities.Clear();
        }
    }

    public void BindListener(IRenderComponentListener<T> listener) => _listeners.Add(listener);
    public void UnbindListener(IRenderComponentListener<T> listener) => _listeners.Remove(listener);

    public void EnsureCapacity(int amount)
    {
        var length = Count + amount;
        if (Capacity >= length) return;

        var newSize = CapacityUtils.CapacityGrowthToFit(Capacity, length);
        _entities = NativeArray.ReAlloc(_entities, Capacity, newSize, 0, true);
        _components = NativeArray.ReAlloc(_components, Capacity, newSize, 0, true);

        Logger.Log(LogScope.Ecs, $"{typeof(T)}: resized {newSize}", LogLevel.Warn);

        Capacity = newSize;
    }

    public void Dispose()
    {
        NativeArray.DisposeArray(_entities, Capacity * Unsafe.SizeOf<RenderEntityId>(), 0);
        NativeArray.DisposeArray(_components, Capacity * Unsafe.SizeOf<T>(), 0);
        _entities = null;
        _components = null;
        Count = 0;
        Capacity = 0;
    }

}