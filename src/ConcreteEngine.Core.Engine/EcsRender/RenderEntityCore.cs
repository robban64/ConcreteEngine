using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;

namespace ConcreteEngine.Core.Engine.EcsRender;

public sealed unsafe partial class RenderEntityCore : IDisposable
{
    public int Count { get; private set; }
    public int Capacity { get; private set; }

    private readonly Stack<int> _free = [];

    internal RenderEntityCore(int initialCapacity)
    {
        Allocate(initialCapacity);
    }

    public int FreeCount => _free.Count;
    public int ActiveCount => Count - _free.Count;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(RenderEntity e) => (uint)e.Index < (uint)Count && _policies[e.Index].Status != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(RenderEntity e) => (uint)e.Index < (uint)Count && _visibilityMask[e.Index] != 0;

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStatus(RenderEntity entity, EntityDrawStatus status)
    {
        if (!IsAlive(entity)) Throwers.InvalidOperation(nameof(entity));
        ref var policy = ref GetDrawPolicy(entity);
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
        
        if (_policies[index].Status != 0) Throwers.InvalidOperation("Entity already exists");
        
        _policies[index] = policy;
        _sources[index] = source;
        var gen = ++_generations[index];

        var entity = new RenderEntity(index, gen);
        ClearEntitySpatial(entity);
        return entity;
    }

    public void Remove(RenderEntity entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Entity, nameof(entity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(entity.Entity, Count, nameof(entity));

        if (!IsAlive(entity)) Throwers.InvalidArgument(nameof(entity));

        ClearEntityHeader(entity);
        ClearEntitySpatial(entity);

        Count = SlotHelper.FreeSlot(_free, entity.Index, Count);
    }
}