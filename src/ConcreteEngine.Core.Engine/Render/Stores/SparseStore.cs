using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;

public sealed class SparseStore<T> : RenderStore where T : unmanaged, IRenderComponent<T>
{
    // ReSharper disable once StaticMemberInGenericType
    public static bool IsActive { get; private set; }

    private static int GetAllocSize(int length) => length * (Unsafe.SizeOf<RenderEntity>() + Unsafe.SizeOf<T>());

    public bool IsDirty { get; private set; }
    public int Count { get; private set; }

    private NativeArray<byte> _memory;

    private NativeView<T> _components;
    private NativeView<RenderEntity> _entities;

    private BitSet _entitySet;

    private readonly List<RenderEntity> _removedEntities = [];
    private readonly List<IRenderComponentListener<T>> _listeners = [];

    public SparseStore(int initialCapacity, int coreCapacity)
    {
        if (IsActive) Throwers.InvalidOperation("Already initialized");
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 16);

        IsActive = true;

        _entitySet = new BitSet(coreCapacity);

        _memory = NativeArray.Allocate(GetAllocSize(initialCapacity));

        var allocator = new NativeAllocBuilder(_memory);
        _entities = allocator.AllocSlice<RenderEntity>(initialCapacity);
        _components = allocator.AllocSlice<T>(initialCapacity);
    }

    public int Capacity => _entities.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> ComponentSpan() => _components.AsSpan(0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<RenderEntity> EntitySpan() => _entities.AsSpan(0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndex(RenderEntity entity)
    {
        if (!entity.IsValid) Throwers.InvalidArgumentHandle(entity);
        var span = _entities.Reinterpret<ulong>().AsReadOnlySpan(0, Count);
        return SearchMethod.BinarySearch(span, RenderEntity.Pack(entity));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndexLinear(RenderEntity entity)
    {
        if (!entity.IsValid) Throwers.InvalidArgumentHandle(entity);
        var span = _entities.Reinterpret<ulong>().AsReadOnlySpan(0, Count);
        return span.IndexOf(RenderEntity.Pack(entity));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(RenderEntity entity) => _entitySet[entity.Id];

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
    public ref T GetUnchecked(int entity)
    {
        if (!_entitySet[entity]) Throwers.InvalidArgumentHandle(entity);
        var index = SearchMethod.BinarySearch(_entities.AsReadOnlySpan(0, Count), new RenderEntity(entity, 0));
        return ref GetByIndex(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(RenderEntity entity)
    {
        var index = FindIndex(entity);
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


    public bool Add(RenderEntity entity, in T value)
    {
        if (!entity.IsValid || Has(entity)) Throwers.InvalidArgument(nameof(entity));
        if (Count >= Capacity) EnsureCapacity(1);

        var index = Count++;
        _entities[index] = entity;
        _components[index] = value;

        _entitySet.EnableBit(entity.Id);

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
        if (!entity.IsValid || !Has(entity)) Throwers.InvalidArgument(nameof(entity));

        _entitySet.DisableBit(entity.Id);
        _removedEntities.Add(entity);
        IsDirty = true;
        return true;
    }

    public void Commit()
    {
        if (!IsDirty) return;
        IsDirty = false;

        if (_removedEntities.Count > 0) CommitRemoved();

        var entitySpan = EntitySpan();
        entitySpan.Sort(ComponentSpan());

        var entitySet = _entitySet;
        entitySet.Clear();
        foreach (var entity in entitySpan) entitySet.EnableBit(entity.Id);
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
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        
        var capacity = Capacity;
        var required = Count + amount;
        if (capacity >= required) return;

        var newLength = CapacityUtils.CapacityGrowthToFit(capacity, required);

        var srcOffsetInBytes = _entities.SizeInBytes;
        var srcCountInBytes = Count * Unsafe.SizeOf<T>();

        _memory.ReAlloc(GetAllocSize(newLength), true);

        var allocator = new NativeAllocBuilder(_memory);
        var newEntities = allocator.AllocSlice<RenderEntity>(newLength);
        var newComponents = allocator.AllocSlice<T>(newLength);

        var srcSpan = _memory.AsSpan().Slice(srcOffsetInBytes, srcCountInBytes);
        var dstSpan = newComponents.Reinterpret<byte>().AsSpan();
        srcSpan.CopyTo(dstSpan);
        srcSpan.Clear();

        _entities = newEntities;
        _components = newComponents;

        Logger.Log(LogScope.Ecs, $"{typeof(T).Name}: resized {newLength}", LogLevel.Warn);
    }

    internal override void OnDenseResized(int newSize)
    {
        if (newSize > _entitySet.BitCount) _entitySet = _entitySet.Resized(newSize);
    }

    public override void Dispose()
    {
        _memory.Dispose();
        _entities = default;
        _components = default;
        Count = 0;
        IsActive = false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderWorld.Query.ComponentEnumerator<T> GetEnumerator() => new();
}