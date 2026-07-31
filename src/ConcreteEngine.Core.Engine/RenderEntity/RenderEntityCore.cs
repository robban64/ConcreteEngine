using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public sealed unsafe partial class RenderEntityCore : IDisposable
{
    public int Count { get; private set; }
    public int Capacity { get; private set; }

    private readonly Stack<int> _free = [];

    internal RenderEntityCore(int initialCapacity)
    {
        Allocate(initialCapacity);
    }

    public int ActiveCount => Count - _free.Count;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(RenderEntityId e)
    {
        var index = e.Index();
        return (uint)index < (uint)Capacity && _headers[index].Status != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(RenderEntityId e)
    {
        var index = e.Index();
        return (uint)index < (uint)Capacity && _headers[index].Visible;
    }

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStatus(RenderEntityId entity, EntityStatus status)
    {
        if (!IsAlive(entity)) Throwers.InvalidOperation(nameof(entity));
        _headers[entity.Index()].Status = status;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleDrawFlag(RenderEntityId entity, EntityDrawFlags flag, bool enabled)
    {
        if (!IsAlive(entity)) Throwers.InvalidOperation(nameof(entity));
        if (enabled) GetSource(entity).DrawFlags |= flag;
        else GetSource(entity).DrawFlags &= ~flag;
    }


    public RenderEntityId AddEntity(RenderSource source, DrawPolicy policy)
    {
        ValidateSource(source);

        var index = SlotHelper.NextSlot(_free, Count);
        if (index < 0)
        {
            if (Count >= Capacity) EnsureCapacity(1);
            index = Count++;
        }
        
        var entity = new RenderEntityId(index + 1);

        if (_headers[index].Status != 0) Throwers.InvalidOperation("Entity already exists");
        _headers[index] = new EntityHeader {Status = EntityStatus.Normal, Visible = true};
        _sources[index] = source;
        _policies[index] = policy;
        ClearEntitySpatial(entity);
        
        return entity;
    }

    public void Remove(RenderEntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(entity.Id, Count, nameof(entity));

        if (!IsAlive(entity)) Throwers.InvalidArgument(nameof(entity));

        ClearEntityHeader(entity);
        ClearEntitySpatial(entity);

        Count = SlotHelper.FreeSlot(_free, entity.Index(), Count);
    }


    [StackTraceHidden]
    private static void ValidateSource(RenderSource source)
    {
        return;
        //if (source.Kind == EntitySourceKind.Particle) return;
        ArgumentOutOfRangeException.ThrowIfZero(source.Mesh.Id, nameof(source.Mesh));
        ArgumentOutOfRangeException.ThrowIfZero(source.Material.Value, nameof(source.Material));
    }
}
