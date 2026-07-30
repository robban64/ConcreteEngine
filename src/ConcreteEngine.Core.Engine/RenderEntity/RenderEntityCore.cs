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
    private readonly List<Action<int>> _resizeCallbacks = [];

    internal RenderEntityCore(int initialCapacity)
    {
        Allocate(initialCapacity);
    }

    public int ActiveCount => Count - _free.Count;

    public void AddResizeCallback(Action<int> callback) => _resizeCallbacks.Add(callback);
    public void RemoveResizeCallback(Action<int> callback) => _resizeCallbacks.Remove(callback);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(RenderEntityId e)
    {
        var index = e.Index();
        return (uint)index < (uint)Capacity && _meta[index] != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(RenderEntityId e)
    {
        var index = e.Index();
        return (uint)index < (uint)Capacity && _meta[index] >= EntityStatus.Normal;
    }

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStatus(RenderEntityId entity, EntityStatus status)
    {
        if (!IsAlive(entity)) Throwers.InvalidOperation(nameof(entity));
        _meta[entity.Index()] = status;
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

        if (_meta[index] != 0) Throwers.InvalidOperation("Entity already exists");
        _meta[index] = EntityStatus.Normal;
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
        if (source.Kind == EntitySourceKind.Particle) return;
        ArgumentOutOfRangeException.ThrowIfZero(source.Mesh.Id, nameof(source.Mesh));
        ArgumentOutOfRangeException.ThrowIfZero(source.Material.Value, nameof(source.Material));
        ArgumentOutOfRangeException.ThrowIfEqual((int)source.Kind, (int)EntitySourceKind.Unknown, nameof(source.Kind));
    }
}
