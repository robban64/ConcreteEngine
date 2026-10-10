using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Render.Components;
using ConcreteEngine.Core.Engine.Render.Systems;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld : IDisposable
{
    public static int EntityCount => Instance.Count;
    public static int EntityCapacity => Instance.Capacity;
    public static int EntityChunkCount => (EntityCount + 255) >> 8;
    public static int EntityBlockCount =>  (EntityCount + 63) >> 6;


    public static RenderMetaStore Meta => Instance._meta;
    public static RenderWorld Instance { get; private set; } = null!;

    //
    public int Count { get; private set; }
    public int Capacity { get; private set; }

    private readonly RenderMetaStore _meta;
    
    private readonly List<RenderStore> _stores = new(8);
    private readonly List<RenderStore> _denseStores = new(8);
    private readonly List<RenderWorldSystem> _systems = new(8);
    private readonly List<RenderBitSet> _bitSets = new(16);
    private readonly Stack<int> _free = [];

    internal RenderWorld(int initialCapacity = 1024)
    {
        if (Instance != null!) Throwers.InvalidOperation();
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 64);
        ArgumentOutOfRangeException.ThrowIfNotEqual(IntMath.AlignUp(initialCapacity, 64), IntMath.AlignDown(initialCapacity, 64));
        Capacity = initialCapacity;
        Instance = this;
        _meta = new RenderMetaStore(initialCapacity);
    }

    public int FreeCount => _free.Count;
    public int ActiveCount => Count - _free.Count;

    public void Commit()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderEntityContext GetContext(RenderEntity entity)
    {
        ValidateHandle(entity);
        return new RenderEntityContext(entity);
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

        return _meta.AddEntity(entityId, policy, source);
    }

    public void RemoveEntity(RenderEntity entity)
    {
        ValidateHandle(entity);
        _meta.RemoveEntity(entity);
        Count = SlotHelper.FreeSlot(_free, entity.Id, Count);
    }

    private void EnsureCapacity(int amount)
    {
        var required = IntMath.AlignUp(Count + amount, 64);
        if (Capacity >= required) return;

        var newSize = CapacityUtils.CapacityGrowthToFit(Capacity, required);
        Capacity = newSize;
        
        Logger.Log(LogScope.Ecs, "RenderEcs resized", LogLevel.Warn);

        var resizeSets = _meta.Resize(newSize);
        if (resizeSets)
        {
            var bitCount = _meta.EntitySet.BitCapacity;
            foreach (var set in _bitSets) set.Resize(bitCount);
        }
        foreach (var dense in _denseStores) dense.OnDenseResized(newSize);
        foreach (var sparse in _stores) sparse.OnDenseResized(newSize);
        foreach (var system in _systems) system.OnDenseResized(newSize);
    }

    public void Dispose()
    {
        foreach (var system in _systems) system.Dispose();
        foreach (var sparse in _stores) sparse.Dispose();
        foreach (var dense in _denseStores) dense.Dispose();

        _meta.Dispose();
    }

    
    //
    
    internal void SetupTest()
    {
        if (_stores.Count > 0 || _denseStores.Count > 0) throw new InvalidOperationException("ECS already initialized");

        CreateDenseStore<DrawPolicy>(Capacity, true);
        CreateDenseStore<DrawSource>(Capacity, true);
        CreateDenseStore<DrawEntityKey>(Capacity, true); // Remove

        CreateDenseStore<WorldBox>(Capacity, false);
        CreateDenseStore<WorldTransform>(Capacity, false);
        CreateDenseStore<WorldNormal>(Capacity, false);
        
        CreateDenseStore<SceneLink>(Capacity, true);

        CreateComponentStore<DrawInstanced>(32);
        CreateComponentStore<SkinningLink>(16);
        CreateComponentStore<EmitterLink>(16);
        CreateComponentStore<SelectionEffect>(16);
        CreateComponentStore<DebugBoundsEffect>(16);
        
        CreateSystem<RenderCullSystem>();
        CreateSystem<RenderPassSystem>();
    }
    
    //
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SparseStore<T> Sparse<T>() where T : unmanaged, IRenderComponent<T> => SparseStores<T>.Store;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DenseStore<T> Dense<T>() where T : unmanaged, IRenderComponent<T> => DenseStores<T>.Store;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T System<T>() where T : RenderWorldSystem => WorldSystems<T>.System;
    
    private void CreateComponentStore<T>(int capacity) where T : unmanaged, IRenderComponent<T>
    {
        if (SparseStores<T>.Store != null!) Throwers.InvalidOperation();
        SparseStores<T>.Store = new SparseStore<T>(capacity, Capacity);
        _stores.Add(SparseStores<T>.Store);
    }

    private void CreateDenseStore<T>(int capacity, bool zeroed) where T : unmanaged, IRenderComponent<T>
    {
        if (DenseStores<T>.Store != null!) Throwers.InvalidOperation();
        DenseStores<T>.Store = new DenseStore<T>(capacity, zeroed);
        _denseStores.Add(DenseStores<T>.Store);
    }
    
    private void CreateSystem<T>() where T : RenderWorldSystem, new()
    {
        if (WorldSystems<T>.System != null!) Throwers.InvalidOperation();
        WorldSystems<T>.System = new T();
        _systems.Add(WorldSystems<T>.System);
    }

    
    private static class SparseStores<T> where T : unmanaged, IRenderComponent<T>
    {
        public static SparseStore<T> Store = null!;
    }

    private static class DenseStores<T> where T : unmanaged, IRenderComponent<T>
    {
        public static DenseStore<T> Store = null!;
    }
    
    private static class WorldSystems<T> where T : RenderWorldSystem
    {
        public static T System = null!;
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ValidateEntity(RenderEntity entity)
    {
        if ((uint)entity.Id >= (uint)EntityCount) Throwers.InvalidOperation(nameof(entity));
        if(entity.Gen == 0 || entity.Gen != Meta.GetGeneration(entity.Id)) Throwers.InvalidOperation(nameof(entity));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ValidateHandle(RenderEntity entity)
    {
        if ((uint)entity.Id >= (uint)EntityCount || entity.Gen == 0) Throwers.InvalidOperation(nameof(entity));
    }
    
}