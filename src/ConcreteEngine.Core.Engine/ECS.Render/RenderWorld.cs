using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.ECS.Render.Components;
using ConcreteEngine.Core.Engine.ECS.Render.Queries;
using ConcreteEngine.Core.Engine.ECS.Render.Systems;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public sealed class RenderWorld : IDisposable
{
    public static RenderWorld Core { get; private set; } = null!;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RenderComponentStore<T> Store<T>() where T : unmanaged, IRenderComponent<T> => ComponentStores<T>.Store;

    private static class ComponentStores<T> where T : unmanaged, IRenderComponent<T>
    {
        public static RenderComponentStore<T> Store = null!;
    }
    
    public void CreateStore<T>(int capacity) where T : unmanaged, IRenderComponent<T>
    {
        if(ComponentStores<T>.Store != null!) Throwers.InvalidOperation();
        ComponentStores<T>.Store = new RenderComponentStore<T>(capacity, Data.Capacity);
        _stores.Add(ComponentStores<T>.Store);
    }
    
    internal void Init()
    {
        if (_stores.Count > 0) throw new InvalidOperationException("ECS already initialized");
        CreateStore<DrawInstancedComponent>(32);
        CreateStore<SkinningLink>(16);
        CreateStore<EmitterLink>(16);
        CreateStore<SelectionComponent>(16);
        CreateStore<DebugBoundsComponent>(16);
    }

    //
    public int Count { get; private set; }
    
    public readonly RenderData Data;
    public readonly RenderCullSystem CullSystem;
    public readonly RenderPassSystem PassSystem;

    private readonly Stack<int> _free = [];

    private readonly List<RenderStore> _stores = new(8);
    private readonly List<RenderWorldSystem> _systems;
    
    internal RenderWorld(int initialCapacity = 1024)
    {
        if(Core != null!) Throwers.InvalidOperation();
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 64);

        Core = this;
        Data = new RenderData(initialCapacity);
        CullSystem = new RenderCullSystem(Data);
        PassSystem = new RenderPassSystem(Data, CullSystem);
        _systems = [CullSystem, PassSystem];
    }

    public int FreeCount => _free.Count;
    public int ActiveCount => Count - _free.Count;
    public int Capacity => Data.Capacity;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(RenderEntity e) => (uint)e.Id < (uint)Count && Data.IsAlive(e.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(RenderEntity e) => (uint)e.Id < (uint)Count && Data.IsVisible(e.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal DrawEntityContext GetDrawContext(int entity)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)entity, (uint)Count);
        return new DrawEntityContext(entity, Data.GetSource(entity));
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

        foreach (var system in _stores) system.OnCoreResize(newSize);
        foreach (var system in _systems) system.OnCoreResize(newSize);
    }

    public void Dispose()
    {
        foreach (var system in _systems) system.Dispose();
        foreach (var store in _stores) store.Dispose();
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