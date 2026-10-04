using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.ECS.Render.Queries;
using ConcreteEngine.Core.Engine.ECS.Render.Systems;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public sealed class RenderEntityCore : IDisposable
{
    public int Count { get; private set; }

    private readonly Stack<int> _free = [];

    public readonly EntityDataStore Data;
    public readonly RenderCullSystem CullSystem;

    internal RenderEntityCore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 64);
        Data = new EntityDataStore(initialCapacity);
        CullSystem = new RenderCullSystem(Data);
    }

    public int FreeCount => _free.Count;
    public int ActiveCount => Count - _free.Count;
    public int Capacity => Data.Capacity;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(RenderEntity e) => (uint)e.Id < (uint)Count && Data.IsAlive(e.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(RenderEntity e) => (uint)e.Id < (uint)Count && Data.IsVisible(e.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RenderEntityContext GetSimpleContext(int entity)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)entity, (uint)Count);
        return new RenderEntityContext(new RenderEntity(entity, 0), Data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderEntityContext GetContext(RenderEntity entity)
    {
        ValidateHandle(entity);
        return new RenderEntityContext(entity, Data);
    }

    //
    public RenderEntity AddEntity(DrawSource source, DrawPolicy policy)
    {
        var entityId = SlotHelper.NextSlot(_free, Count);
        if (entityId < 0)
        {
            if (Count >= Capacity) EnsureCapacity(1);
            entityId = Count++;
        }

        return Data.AddEntity(entityId, policy, source);
    }

    public void RemoveEntity(RenderEntity entity)
    {
        ValidateHandle(entity);
        Data.RemoveEntity(entity);
        Count = SlotHelper.FreeSlot(_free, entity.Id, Count);
    }

    private void EnsureCapacity(int amount)
    {
        var required = Count + amount;
        if (Capacity >= required) return;

        var newSize = CapacityUtils.CapacityGrowthToFit(Capacity, required);
        Logger.Log(LogScope.Ecs, "RenderEcs resized", LogLevel.Warn);

        Data.ReAlloc(newSize);
    }

    public void Dispose()
    {
        Data.Dispose();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateHandle(RenderEntity entity)
    {
        if((uint)entity.Id >= (uint)Count || entity.Gen == 0) Throwers.InvalidOperation(nameof(entity));
    }
    
    public RenderCoreQuery.VisibilityQueryEnumerator<BoundingAxisBox> VisibilityBoundsQuery() =>
        new(Data.VisibleSet, Data.WorldBounds.Slice(0, Count));

}