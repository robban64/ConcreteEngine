using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.ECS.Data;
using ConcreteEngine.Engine.ECS.Enumerators;
using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS;

public sealed class RenderEntityHub
{
    public const int DefaultEntityCapacity = 1024;
    
    private readonly RenderEntityCore _core;

    public int EntityCount => _core.ActiveCount;
    public RenderEntityCore Core => _core;

    internal RenderEntityHub()
    {
        if (StaticStores.All.Count > 0)
            throw new InvalidOperationException("WorldEntities already initialized");

        _core = new RenderEntityCore(DefaultEntityCapacity);
        GenericStores<RenderAnimationComponent>.CreateStore(64);
        GenericStores<ParticleComponent>.CreateStore(16);
        GenericStores<SelectionComponent>.CreateStore(16);
        GenericStores<DebugBoundsComponent>.CreateStore(16);
    }


    public RenderEntityId AddEntity(in CoreComponentBundle data) => _core.AddEntity(in data);

    public void AddComponent<T>(RenderEntityId entity, in T component) where T : unmanaged, IRenderComponent<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity.Id));
        GenericStores<T>.Store.Add(entity, component);
    }

    public void RemoveComponent<T>(RenderEntityId entity) where T : unmanaged, IRenderComponent<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity.Id));
        GenericStores<T>.Store.Remove(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndTick()
    {
        foreach (var store in StaticStores.All) store.EndTick();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RenderComponentEnumerator<T> Query<T>() where T : unmanaged, IRenderComponent<T> => new(GenericStores<T>.Store);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RenderEntityEnumerator CoreQuery() => new(_core.GetContext());

    public RenderEntityStore<T> GetStore<T>() where T : unmanaged, IRenderComponent<T> => GenericStores<T>.Store;

    private static class StaticStores
    {
        public static readonly List<IRenderEntityStore> All = new (8);
    }
    
    private static class GenericStores<T> where T : unmanaged, IRenderComponent<T>
    {
        public static RenderEntityStore<T> Store = null!;

        public static void CreateStore(int cap)
        {
            var store = new RenderEntityStore<T>(cap);
            StaticStores.All.Add(store);
            Store = store;
        }
    }
}