using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public sealed unsafe partial class RenderEntityCore : IDisposable
{
    public int Count { get; private set; }
    public int Capacity { get; private set; }

    private NativeArray<ushort> _generations;

    private readonly Stack<int> _free = [];

    private readonly EntityDataStore _entityDataStore;

    internal RenderEntityCore(int initialCapacity)
    {
        if (!_generations.IsNull || Capacity != 0) Throwers.InvalidOperation("Already allocated");
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 64);
        Capacity = initialCapacity;

        _generations = NativeArray.Allocate<ushort>(initialCapacity);
        _entityDataStore = new EntityDataStore(initialCapacity);
    }

    public int FreeCount => _free.Count;
    public int ActiveCount => Count - _free.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValidHandle(RenderEntity e) => e.IsValid && (uint)e.Id < (uint)Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(RenderEntity e) => (uint)e.Id < (uint)Count && _entityDataStore.IsAlive(e.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(RenderEntity e) => (uint)e.Id < (uint)Count && _entityDataStore.IsVisible(e.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderEntity CreateHandle(int entityId)
    {
        if((uint)entityId >= (uint)Count) Throwers.IndexOutOfRange(entityId, Count, nameof(entityId));
        return new RenderEntity(entityId, _generations[entityId]);
    }

    //TODO
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderEntityContext GetContext(int entityId)
    {
        return new RenderEntityContext(CreateHandle(entityId), _entityDataStore);
    }
    
    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStatus(RenderEntity entity, EntityDrawStatus status)
    {
        if (!IsAlive(entity)) Throwers.InvalidOperation(nameof(entity));
        ref var policy = ref GetPolicy(entity);
        var newPolicy = policy.WithStatus(status);
        policy = newPolicy;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleDrawFlag(RenderEntity entity, EntityDrawFlags flag, bool enabled)
    {
        if (!IsAlive(entity)) Throwers.InvalidOperation(nameof(entity));
        if (enabled) GetSource(entity).DrawFlags |= flag;
        else GetSource(entity).DrawFlags &= ~flag;
    }


    public RenderEntity AddEntity(DrawSource source, DrawPolicy policy)
    {
        var index = SlotHelper.NextSlot(_free, Count);
        if (index < 0)
        {
            if (Count >= Capacity) EnsureCapacity(1);
            index = Count++;
        }

        var gen = ++_generations[index];
        var entity = new RenderEntity(index, gen);
        _entityDataStore.AddEntity(entity, policy, source);
        return entity;
    }

    public void Remove(RenderEntity entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(entity.Id, Count, nameof(entity));

        if (!IsAlive(entity)) Throwers.InvalidArgument(nameof(entity));

        _entityDataStore.ClearHeader(entity);
        _entityDataStore.ClearSpatial(entity);

        Count = SlotHelper.FreeSlot(_free, entity.Id, Count);
    }

    private void EnsureCapacity(int amount)
    {
        var required = Count + amount;
        if (Capacity >= required) return;

        var newSize = CapacityUtils.CapacityGrowthToFit(Capacity, required);
        Capacity = newSize;

        Logger.Log(LogScope.Ecs, "RenderEcs resized", LogLevel.Warn);

        _generations.ReAlloc(newSize, true);
        _entityDataStore.ReAlloc(newSize);
        RenderEcs.OnResize(newSize);
    }

    public void Dispose()
    {
        _generations.Dispose();
        _entityDataStore.Dispose();
    }
}