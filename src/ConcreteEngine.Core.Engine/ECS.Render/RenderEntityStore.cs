using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public interface IRenderEntityStore : IDisposable;

public sealed unsafe partial class RenderEntityStore<T> : IRenderEntityStore where T : unmanaged, IRenderComponent<T>
{
    public static RenderEntityStore<T> Instance { get; private set; } = null!;

    private static int GetAllocSize(int length) => length * (sizeof(RenderEntity) + Unsafe.SizeOf<T>());

    public bool IsDirty { get; private set; }
    public int Count { get; private set; }
    public int Capacity { get; private set; }

    private T* _components;
    private RenderEntity* _entities;

    private NativeArray<byte> _memory;

    private readonly List<RenderEntity> _removedEntities = [];
    private readonly List<IRenderComponentListener<T>> _listeners = [];

    public RenderEntityStore(int initialCapacity)
    {
        if(Instance != null!) Throwers.InvalidOperation("Already  initialized");
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 16);

        Instance = this;
        
        Capacity = initialCapacity;

        _memory = NativeArray.Allocate(GetAllocSize(initialCapacity));
        var allocator = new NativeAllocBuilder(_memory);
        _entities = allocator.AllocSlice<RenderEntity>(initialCapacity);
        _components = allocator.AllocSlice<T>(initialCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndex(RenderEntity entity) =>
        SearchMethod.BinarySearch(new ReadOnlySpan<RenderEntity>(_entities, Count), entity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndexLinear(RenderEntity entity)
    {
        var span = EntitiesView().Reinterpret<ulong>().AsReadOnlySpan();
        return span.IndexOf(RenderEntity.Pack(entity));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(RenderEntity entity) => FindIndexLinear(entity) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderEntity GetEntity(int index)
    {
        if ((uint)index >= (uint)Count) Throwers.IndexOutOfRange(index, Count, nameof(index));
        return _entities[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetByIndex(int index)
    {
        if ((uint)index >= (uint)Count) Throwers.IndexOutOfRange(index, Count, nameof(index));
        return ref _components[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(RenderEntity entity) => ref GetByIndex(FindIndex(entity));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetUnchecked(int entity)
    {
        var index = FindIndex(new RenderEntity(entity, 0));
        return ref GetByIndex(index);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOrDefault(RenderEntity entity)
    {
        var index = FindIndex(entity);
        if ((uint)index >= (uint)Count) return default;
        return _components[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(RenderEntity entity, out ValueRef<T> value)
    {
        var index = FindIndex(entity);
        if ((uint)index < (uint)Count)
        {
            value = new ValueRef<T>(ref _components[index]);
            return true;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<RenderEntity> EntitiesView() => new(_entities, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<T> ComponentsView() => new(_components, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<RenderEntity> EntitySpan() => new(_entities, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> ComponentSpan() => new(_components, Count);

    public bool Add(RenderEntity entity, in T value)
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

    public bool Remove(RenderEntity entity)
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
    }

    private void CommitRemoved()
    {
        var entities = _entities;
        var components = _components;
        foreach (var removed in CollectionsMarshal.AsSpan(_removedEntities))
        {
            var index = FindIndexLinear(removed);
            if (_listeners.Count > 0)
            {
                foreach (var listener in CollectionsMarshal.AsSpan(_listeners))
                    listener.ComponentRemoved(removed, ref components[index]);
            }

            var count = --Count;
            entities[index] = entities[count];
            components[index] = components[count];

            entities[count] = default;
            components[count] = default;
        }

        _removedEntities.Clear();
    }


    public void BindListener(IRenderComponentListener<T> listener) => _listeners.Add(listener);
    public void UnbindListener(IRenderComponentListener<T> listener) => _listeners.Remove(listener);

    public void EnsureCapacity(int amount)
    {
        var length = Count + amount;
        if (Capacity >= length) return;

        var newLength = CapacityUtils.CapacityGrowthToFit(Capacity, length);
        _memory.ReAlloc(GetAllocSize(newLength), true);

        Logger.Log(LogScope.Ecs, $"{nameof(T)}: resized {newLength}", LogLevel.Warn);

        Capacity = newLength;
    }

    public void Dispose()
    {
        _memory.Dispose();
        _memory = default;
        _entities = null;
        _components = null;
        Count = 0;
        Capacity = 0;
    }
}