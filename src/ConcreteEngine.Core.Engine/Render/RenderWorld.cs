using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Render.Components;
using ConcreteEngine.Core.Engine.Render.Systems;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld : IDisposable
{
    public static int EntityCount => Instance.Count;
    public static RenderMetaStore MetaStore => Instance.Meta;
    public static RenderWorld Instance { get; private set; } = null!;

    //
    public int Count { get; private set; }

    public readonly RenderMetaStore Meta;
    public readonly RenderCullSystem CullSystem;
    public readonly RenderPassSystem PassSystem;

    private readonly Stack<int> _free = [];

    private readonly List<RenderStore> _stores = new(8);
    private readonly List<RenderStore> _denseStores = new(8);

    private readonly List<RenderWorldSystem> _systems;

    internal RenderWorld(int initialCapacity = 1024)
    {
        if (Instance != null!) Throwers.InvalidOperation();
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 64);

        Instance = this;
        Meta = new RenderMetaStore(initialCapacity);
        CullSystem = new RenderCullSystem(Meta);
        PassSystem = new RenderPassSystem(Meta, CullSystem);
        _systems = [CullSystem, PassSystem];
    }

    public int FreeCount => _free.Count;
    public int ActiveCount => Count - _free.Count;
    public int Capacity => Meta.Capacity;

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

        return Meta.AddEntity(entityId, policy, source);
    }

    public void RemoveEntity(RenderEntity entity)
    {
        ValidateHandle(entity);
        Meta.RemoveEntity(entity);
        Count = SlotHelper.FreeSlot(_free, entity.Id, Count);
    }

    private void EnsureCapacity(int amount)
    {
        var required = Count + amount;
        if (Capacity >= required) return;

        var newSize = CapacityUtils.CapacityGrowthToFit(Capacity, required);
        Logger.Log(LogScope.Ecs, "RenderEcs resized", LogLevel.Warn);

        Meta.Resize(newSize);

        foreach (var dense in _denseStores) dense.OnDenseResized(newSize);
        foreach (var sparse in _stores) sparse.OnDenseResized(newSize);
        foreach (var system in _systems) system.OnDenseResized(newSize);
    }

    public void Dispose()
    {
        foreach (var system in _systems) system.Dispose();
        foreach (var sparse in _stores) sparse.Dispose();
        foreach (var dense in _denseStores) dense.Dispose();

        Meta.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateHandle(RenderEntity entity)
    {
        if ((uint)entity.Id >= (uint)Count || entity.Gen == 0) Throwers.InvalidOperation(nameof(entity));
    }
    
    
    //
    
    internal void SetupTestStores()
    {
        if (_stores.Count > 0 || _denseStores.Count > 0) throw new InvalidOperationException("ECS already initialized");

        CreateDenseStore<DrawPolicy>(Capacity, true);
        CreateDenseStore<DrawSource>(Capacity, true);
        CreateDenseStore<WorldBox>(Capacity, false);
        CreateDenseStore<WorldTransform>(Capacity, false);
        CreateDenseStore<NormalMatrix>(Capacity, false);

        CreateComponentStore<DrawInstancedComponent>(32);
        CreateComponentStore<SkinningLink>(16);
        CreateComponentStore<EmitterLink>(16);
        CreateComponentStore<SelectionComponent>(16);
        CreateComponentStore<DebugBoundsComponent>(16);
    }
    
    //
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SparseStore<T> Sparse<T>() where T : unmanaged, IRenderComponent<T> => SparseStores<T>.Store;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DenseStore<T> Dense<T>() where T : unmanaged, IRenderComponent<T> => DenseStores<T>.Store;
    
    private void CreateComponentStore<T>(int capacity) where T : unmanaged, IRenderComponent<T>
    {
        if (SparseStores<T>.Store != null!) Throwers.InvalidOperation();
        SparseStores<T>.Store = new SparseStore<T>(capacity, Meta.Capacity);
        _stores.Add(SparseStores<T>.Store);
    }

    private void CreateDenseStore<T>(int capacity, bool zeroed) where T : unmanaged, IRenderComponent<T>
    {
        if (DenseStores<T>.Store != null!) Throwers.InvalidOperation();
        DenseStores<T>.Store = new DenseStore<T>(capacity, zeroed);
        _denseStores.Add(DenseStores<T>.Store);
    }
    
    private static class SparseStores<T> where T : unmanaged, IRenderComponent<T>
    {
        public static SparseStore<T> Store = null!;
    }

    private static class DenseStores<T> where T : unmanaged, IRenderComponent<T>
    {
        public static DenseStore<T> Store = null!;
    }

}